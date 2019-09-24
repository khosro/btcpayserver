using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.HostedServices;
using NBitcoin;
using Nethereum.HdWallet;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Transaction = Nethereum.RPC.Eth.DTOs.Transaction;
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
        StreamingWebSocketClient _streamingWebSocketClient;

        public EthereumClient(Uri rpcUri, string websocketUrl, EthereumLikecBtcPayNetwork network, BTCPayNetworkProvider networkProvider, EthereumClientTransactionRepository ethereumClientTransactionRepository)
        {
            _Network = network;
            _rpcUri = rpcUri;
            _NetworkProvider = networkProvider;
            _web3 = new Web3(_rpcUri.AbsoluteUri);
            this._ethereumClientTransactionRepository = ethereumClientTransactionRepository;
            _streamingWebSocketClient = new StreamingWebSocketClient(websocketUrl);
            PendingTransactionsSubscription = new EthNewPendingTransactionObservableSubscription(_streamingWebSocketClient);
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

        public async Task<Transaction> GetTransactionByNumber(string txid)
        {
            return await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txid).ConfigureAwait(false);
        }


        public async Task<Transaction> GetTransactionAsync(string txId, CancellationToken cancellation = default)
        {
            return null;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(EthereumSupportedPaymentMethod paymentMethod, CancellationToken cancellation = default)
        {
            Wallet wallet = new Wallet(paymentMethod.Mnemonic, null);
            List<string> accounts = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                Account account = wallet.GetAccount(i);
                accounts.Add(account.Address);
            }

            return await _ethereumClientTransactionRepository.FindTransactionByAddresses(accounts);
        }

        public Task BroadcastAsync(Transaction tx, CancellationToken cancellation = default)
        {
            return null;
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
    }

    internal class Eth_BlockNumber
    {
        public string jsonrpc { get; set; }
        public string result { get; set; }
        public long LastBlockNumber => Convert.ToInt64(result, 16);
        public int id { get; set; }
    }
}
