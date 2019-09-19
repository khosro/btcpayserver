using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Ethereum.Services.Wallet
{

    public class EthereumWalletProvider
    {
        private EthereumClientProvider _EthereumClientProvider;
        private readonly IOptions<MemoryCacheOptions> _Options;
        public EthereumWalletProvider(EthereumClientProvider ethereumClientProvider,
                                    IOptions<MemoryCacheOptions> memoryCacheOption,
                                    BTCPayNetworkProvider networkProvider)
        {
            if (ethereumClientProvider == null)
            {
                throw new ArgumentNullException(nameof(ethereumClientProvider));
            }

            _EthereumClientProvider = ethereumClientProvider;
            _Options = memoryCacheOption;

            foreach (EthereumLikecBtcPayNetwork network in networkProvider.GetAll().OfType<EthereumLikecBtcPayNetwork>())
            {
                Client.EthereumClient ethereumClient = _EthereumClientProvider.GetEthereumClient(network.CryptoCode);
                if (ethereumClient == null)
                {
                    continue;
                }

                _Wallets.Add(network.CryptoCode, new EthereumWallet(ethereumClient, new MemoryCache(_Options), network));
            }
        }

        private Dictionary<string, EthereumWallet> _Wallets = new Dictionary<string, EthereumWallet>();

        public EthereumWallet GetWallet(BTCPayNetworkBase network)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            return GetWallet(network.CryptoCode);
        }
        public EthereumWallet GetWallet(string cryptoCode)
        {
            if (cryptoCode == null)
            {
                throw new ArgumentNullException(nameof(cryptoCode));
            }

            _Wallets.TryGetValue(cryptoCode, out EthereumWallet result);
            return result;
        }

        public bool IsAvailable(EthereumLikecBtcPayNetwork network)
        {
            return _EthereumClientProvider.IsAvailable(network);
        }

        public IEnumerable<EthereumWallet> GetWallets()
        {
            foreach (KeyValuePair<string, EthereumWallet> w in _Wallets)
            {
                yield return w.Value;
            }
        }
    }
}

