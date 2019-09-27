using System;
using System.Collections.Generic;
using System.Linq;
using BTCPayServer.Ethereum;
using BTCPayServer.Ethereum.HostedServices;
using BTCPayServer.Ethereum.Services.Wallet;
using BTCPayServer.Ethereum.UiUtil;
using BTCPayServer.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
namespace BTCPayServer
{
    public static class EthereumExtensions
    {
        private const string EthereumClientDbInfo = "for EthereumClient";

        public static IServiceCollection AddEthereumLike(this IServiceCollection services)
        {
            //services.AddSingleton(s => s.ConfigureEthereumConfiguration());
            //services.AddSingleton<IHostedService, EthereumListener>();
            services.TryAddSingleton<EthereumExplorerClientProvider>();
            services.TryAddSingleton<EthereumWalletProvider>();
            services.TryAddSingleton<EthereumDashboard>();
            services.TryAddSingleton<EthereumUiUtilService>();
            services.AddSingleton<IHostedService, EthereumWaiters>();
            services.AddSingleton<IHostedService, EthereumListener>();

            //Database(services);

            //services.AddStartupTask<EthereumDataMigrationStartupTask>();

            return services;
        }

        public static EthereumLikecBtcPayNetwork GetEthNetwork(this BTCPayNetworkProvider bTCPayNetworkProvider, string cryptoCode)
        {
            IEnumerable<EthereumLikecBtcPayNetwork> ethNetworks = bTCPayNetworkProvider.GetEthNetworks();
            return ethNetworks.SingleOrDefault(t => t.CryptoCode.Equals(cryptoCode, StringComparison.InvariantCulture));
        }

        public static IEnumerable<EthereumLikecBtcPayNetwork> GetEthNetworks(this BTCPayNetworkProvider bTCPayNetworkProvider)
        {
            IEnumerable<EthereumLikecBtcPayNetwork> ethNetworks = bTCPayNetworkProvider.GetAll().OfType<EthereumLikecBtcPayNetwork>();
            return ethNetworks;
        }

        public static IEnumerable<string> GetEthCryptoCodes(this BTCPayNetworkProvider bTCPayNetworkProvider)
        {
            return bTCPayNetworkProvider.GetEthNetworks().Select(t => t.CryptoCode);
        }
    }
}
