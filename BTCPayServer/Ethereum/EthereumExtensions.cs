using System;
using System.Linq;
using BTCPayServer.Configuration;
using BTCPayServer.Ethereum.Config;
using BTCPayServer.Ethereum.HostedServices;
using BTCPayServer.Ethereum.Services.Wallet;
using BTCPayServer.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BTCPayServer
{
    public static class EthereumExtensions
    {
        public static IServiceCollection AddEthereumLike(this IServiceCollection services)
        {
            services.AddSingleton(s => s.ConfigureEthereumConfiguration());
            services.AddSingleton<IHostedService, EthereumListener>();
            services.TryAddSingleton<EthereumClientProvider>();
            services.TryAddSingleton<EthereumWalletProvider>();
            services.TryAddSingleton<EthereumDashboard>();
            services.AddSingleton<IHostedService, EthereumWaiters>();

            return services;
        }

        private static EthereumOptions ConfigureEthereumConfiguration(this IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            BTCPayNetworkProvider btcPayNetworkProvider = serviceProvider.GetService<BTCPayNetworkProvider>();
            var result = new EthereumOptions();

            System.Collections.Generic.IEnumerable<string> supportedChains = configuration.GetOrDefault<string>("chains", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToUpperInvariant());

            System.Collections.Generic.IEnumerable<EthereumLikecBtcPayNetwork> supportedNetworks = btcPayNetworkProvider.Filter(supportedChains.ToArray()).GetAll()
                .OfType<EthereumLikecBtcPayNetwork>();

            foreach (EthereumLikecBtcPayNetwork net in supportedNetworks)
            {
                Uri rpcUri = configuration.GetOrDefault<Uri>($"{net.CryptoCode}.eth.rcpurl", null);

                if (rpcUri == null)
                {
                    throw new ConfigException($"{net.CryptoCode} is misconfigured");
                }

                result.EthereumConfigs.Add(new EthereumConfig { RpcUri = rpcUri, CryptoCode = net.CryptoCode });
            }
            return result;
        }

    }
}
