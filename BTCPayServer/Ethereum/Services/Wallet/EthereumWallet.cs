using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Ethereum.Client;
using BTCPayServer.Ethereum.Payments;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.RPC.Eth.DTOs;

namespace BTCPayServer.Ethereum.Services.Wallet
{
    //public class ReceivedCoin
    //{
    //    public Coin Coin { get; set; }
    //    public DateTimeOffset Timestamp { get; set; }
    //    public KeyPath KeyPath { get; set; }
    //}
    //public class NetworkCoins
    //{
    //    public class TimestampedCoin
    //    {
    //        public DateTimeOffset DateTime { get; set; }
    //        public Coin Coin { get; set; }
    //    }
    //    public TimestampedCoin[] TimestampedCoins { get; set; }
    //    public DerivationStrategyBase Strategy { get; set; }
    //    public EthereumWallet Wallet { get; set; }
    //}
    public class EthereumWallet
    {
        private EthereumClient _Client;
        private IMemoryCache _MemoryCache;
        public EthereumWallet(EthereumClient client, IMemoryCache memoryCache, EthereumLikecBtcPayNetwork network)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (memoryCache == null)
            {
                throw new ArgumentNullException(nameof(memoryCache));
            }

            _Client = client;
            _Network = network;
            _MemoryCache = memoryCache;
        }


        private readonly EthereumLikecBtcPayNetwork _Network;
        public EthereumLikecBtcPayNetwork Network
        {
            get
            {
                return _Network;
            }
        }

        public TimeSpan CacheSpan { get; private set; } = TimeSpan.FromMinutes(5);

        public async Task<Transaction> GetTransactionAsync(string txId, CancellationToken cancellation = default(CancellationToken))
        {
            if (txId == null)
            {
                throw new ArgumentNullException(nameof(txId));
            }

            Transaction tx = await _Client.GetTransactionAsync(txId, cancellation);
            return tx;
        }

        public Task<IEnumerable<Transaction>> FetchTransactions(EthereumSupportedPaymentMethod paymentMethod)
        {
            return _Client.GetTransactionsAsync(paymentMethod);
        }

        //TODO.Need impl
        //public Task<BroadcastResult[]> BroadcastTransactionsAsync(List<Transaction> transactions)
        //{
        //    Task<BroadcastResult>[] tasks = transactions.Select(t => _Client.BroadcastAsync(t)).ToArray();
        //    return Task.WhenAll(tasks);
        //}

        //TODO.Change impl.
        //public async Task<Money> GetBalance(DerivationStrategyBase derivationStrategy, CancellationToken cancellation = default(CancellationToken))
        //{
        //    return null;
        //}

        //public void InvalidateCache(EthereumSupportedPaymentMethod strategy)
        //{
        //    _MemoryCache.Remove("CACHEDCOINS_" + strategy.ToString());
        //    _FetchingUTXOs.TryRemove(strategy.ToString(), out TaskCompletionSource<UTXOChanges> unused);
        //}

        //private ConcurrentDictionary<string, TaskCompletionSource<UTXOChanges>> _FetchingUTXOs = new ConcurrentDictionary<string, TaskCompletionSource<UTXOChanges>>();
    }
}
