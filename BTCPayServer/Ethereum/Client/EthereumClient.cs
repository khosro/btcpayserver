using System;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.HostedServices;
using NBitcoin;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
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
        public EthereumClient(Uri rpcUri, EthereumLikecBtcPayNetwork network, BTCPayNetworkProvider networkProvider)
        {
            _Network = network;
            _rpcUri = rpcUri;
            _NetworkProvider = networkProvider;
            _web3 = new Web3(_rpcUri.AbsoluteUri);
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

        public Task TrackAsync(DerivationStrategyBase strategy, CancellationToken cancellation = default)
        {
            return null;
        }

        public async Task<TransactionResult> GetTransactionAsync(uint256 txId, CancellationToken cancellation = default)
        {
            return null;
        }

        public Task<GetTransactionsResponse> GetTransactionsAsync(DerivationStrategyBase strategy, CancellationToken cancellation = default)
        {
            return null;
        }

        public Task<BroadcastResult> BroadcastAsync(Transaction tx, CancellationToken cancellation = default)
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
