using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Ethereum.Model;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.HostedServices;
using Ethereum.Client.Services;
using NBitcoin;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
namespace BTCPayServer.Ethereum.Client
{
    public class EthereumClient
    {
        private readonly Uri _rpcUri;
        private readonly Web3 _web3;
        private readonly EthereumLikecBtcPayNetwork _Network;
        private readonly BTCPayNetworkProvider _NetworkProvider;
        private readonly EthereumClientTransactionRepository _ethereumClientTransactionRepository;
        public EthNewPendingTransactionObservableSubscription PendingTransactionsSubscription { get; private set; }

        private StreamingWebSocketClient _streamingWebSocketClient;

        public EthereumClient(Uri rpcUri, string websocketUrl, EthereumLikecBtcPayNetwork network, BTCPayNetworkProvider networkProvider, EthereumClientTransactionRepository ethereumClientTransactionRepository)
        {
            _Network = network;
            _rpcUri = rpcUri;
            _NetworkProvider = networkProvider;
            _web3 = new Web3(_rpcUri.AbsoluteUri);
            _ethereumClientTransactionRepository = ethereumClientTransactionRepository;
            _streamingWebSocketClient = new StreamingWebSocketClient(websocketUrl);
            PendingTransactionsSubscription = new EthNewPendingTransactionObservableSubscription(_streamingWebSocketClient);
            _streamingWebSocketClient.StartAsync().Wait();
            PendingTransactionsSubscription.SubscribeAsync().Wait();
        }

        public async Task<EthereumStatusResult> GetStatusAsync(CancellationToken cancellation = default)
        {
            HexBigInteger localEthBlockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().ConfigureAwait(false);
            string url = "";

            if (_NetworkProvider.NetworkType.Equals(NetworkType.Testnet))//TODO.Move them to config
            {
                url = "https://api-ropsten.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=YourApiKeyToken";
            }
            else if (_NetworkProvider.NetworkType.Equals(NetworkType.Mainnet))
            {
                url = "https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=YourApiKeyToken";
            }

            long etherScanLastBlocknumber = await GetLastedBlockAsync(url);
            BigInteger diff = etherScanLastBlocknumber - localEthBlockNumber.Value;
            if (diff < 0)
            {
                diff *= -1;
            }
            return new EthereumStatusResult()
            {
                ChainHeight = etherScanLastBlocknumber == 0 /*RegTest*/? localEthBlockNumber.Value : etherScanLastBlocknumber,
                CurrentHeight = localEthBlockNumber.Value,
                CryptoCode = _Network.CryptoCode,
                IsFullySynched = etherScanLastBlocknumber == 0 /*RegTest*/ ? true : (diff > 1/*This value is based on experience*/ ? false : true)
            };
        }

        public async Task<HexBigInteger> GetBlockNumber()
        {
            return await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().ConfigureAwait(false);
        }

        public async Task<BlockWithTransactions> GetBlockWithTransactionsByNumber(BigInteger blockNumber)
        {
            return await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(blockNumber)).ConfigureAwait(false);
        }

        public async Task<EthereumClientTransactionData> GetTransactionAsyncByTransactionId(string txid)
        {
            var trans = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txid).ConfigureAwait(false);
            var transData = new EthereumClientTransactionData();
            transData.Init(trans);
            return transData;
        }

        public async Task<Dictionary<string, decimal>> GetBalanceByMnemonic(string mnemonic)
        {
            return await GetBalances(AddressPoolService.GenerateAddressByMnemonic(mnemonic));
        }

        public async Task<Dictionary<string, decimal>> GetBalances(IEnumerable<string> addresses)
        {
            Dictionary<string, decimal> address2Balance = new Dictionary<string, decimal>();
            foreach (var addresse in addresses)
            {
                address2Balance.Add(addresse, await GetBalance(addresse));
            }
            return address2Balance;
        }

        public async Task<Decimal> GetBalance(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
            return Web3.Convert.FromWei(balance.Value);
        }

        public async Task<IEnumerable<EthereumClientTransactionData>> GetTransactionsAsync(EthereumSupportedPaymentMethod paymentMethod)
        {
            var accounts = AddressPoolService.GenerateAddressByMnemonic(paymentMethod.Mnemonic);

            return await _ethereumClientTransactionRepository.FindTransactionByAddresses(accounts);
        }

        private async Task<long> GetLastedBlockAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return 0;//In RegTest, it is null or empty
            }
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            // Above three lines can be replaced with new helper method below
            // string responseBody = await client.GetStringAsync(uri);
            Eth_BlockNumber eth_BlockNumber = JsonConvert.DeserializeObject<Eth_BlockNumber>(responseBody);
            return eth_BlockNumber.LastBlockNumber;
        }

        public async Task<string> BroadcastAsync(EthWalletSendModel ethWalletSendModel, string mnemonic)
        {
            var transactionInput =
               new TransactionInput
               {
                   Value = new HexBigInteger(Web3.Convert.ToWei(ethWalletSendModel.AmountInEther)),
                   To = ethWalletSendModel.AddressTo,
                   From = ethWalletSendModel.SelectedAccount
               };
            if (ethWalletSendModel.Gas != null)
                transactionInput.Gas = new HexBigInteger(ethWalletSendModel.Gas.Value);
            if (!string.IsNullOrEmpty(ethWalletSendModel.GasPrice))
            {
                var parsed = decimal.Parse(ethWalletSendModel.GasPrice, CultureInfo.InvariantCulture);
                transactionInput.GasPrice = new HexBigInteger(Web3.Convert.ToWei(ethWalletSendModel.GasPrice, UnitConversion.EthUnit.Gwei));
            }

            if (ethWalletSendModel.Nonce != null)
                transactionInput.Nonce = new HexBigInteger(ethWalletSendModel.Nonce.Value);
            if (!string.IsNullOrEmpty(ethWalletSendModel.Data))
                transactionInput.Data = ethWalletSendModel.Data;

            var account = AddressPoolService.GenerateAddressInfoByMnemonic(mnemonic).SingleOrDefault(t => t.Key.Equals(ethWalletSendModel.SelectedAccount.ToLowerInvariant(), StringComparison.InvariantCulture)).Value;
            if (account != null)
            {
                var privateKey = account;
                var web3 = new Web3(new Account(privateKey), _rpcUri.AbsoluteUri);

                var txnHash = await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput).ConfigureAwait(false);
                return txnHash;
            }
            throw new ArgumentException($@"Account address: {transactionInput.From}, not found", nameof(transactionInput));
        }
    }

    internal class Eth_BlockNumber
    {
        public string jsonrpc { get; set; }
        public string result { get; set; }
        public long LastBlockNumber => Convert.ToInt64(result, 16);
        public int id { get; set; }
    }
}
