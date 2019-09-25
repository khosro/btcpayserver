using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Ethereum.Client;
using BTCPayServer.Ethereum.Model;
using BTCPayServer.Ethereum.Payments;
using Microsoft.Extensions.Caching.Memory;

namespace BTCPayServer.Ethereum.Services.Wallet
{
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
            Network = network;
            _MemoryCache = memoryCache;
        }
        public EthereumLikecBtcPayNetwork Network { get; }

        public async Task<EthereumClientTransactionData> GetTransactionAsyncByTransactionId(string txId)
        {
            if (txId == null)
            {
                throw new ArgumentNullException(nameof(txId));
            }

            EthereumClientTransactionData tx = await _Client.GetTransactionAsyncByTransactionId(txId);
            return tx;
        }

        public Task<IEnumerable<EthereumClientTransactionData>> FetchTransactions(EthereumSupportedPaymentMethod paymentMethod)
        {
            return _Client.GetTransactionsAsync(paymentMethod);
        }

        public async Task<Decimal> GetBalance(string address)
        {
            return await _Client.GetBalance(address);
        }

        public async Task<Dictionary<string, decimal>> GetBalanceByMnemonic(string mnemonic)
        {
            return await _Client.GetBalanceByMnemonic(mnemonic);
        }

        public async Task<Dictionary<string, decimal>> GetBalances(IEnumerable<string> addresses)
        {
            return await _Client.GetBalances(addresses);

        }

        public async Task<string> BroadcastAsync(EthWalletSendModel ethWalletSendModel, string mnemonic)
        {
            return await _Client.BroadcastAsync(ethWalletSendModel, mnemonic);
        }
    }
}
