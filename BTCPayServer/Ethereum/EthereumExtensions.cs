using System;
using System.Collections.Generic;
using System.Linq;
using BTCPayServer.Configuration;
using BTCPayServer.Ethereum;
using BTCPayServer.Ethereum.HostedServices;
using BTCPayServer.Ethereum.Services.Wallet;
using BTCPayServer.Ethereum.UiUtil;
using BTCPayServer.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BTCPayServer
{
    public class EthereumOptions
    {
        public EthereumOptions()
        {
            EthereumNBXplorerOptions = new List<EthereumNBXplorerOption>();
        }
        public List<EthereumNBXplorerOption> EthereumNBXplorerOptions { get; set; }
    }

    public class EthereumNBXplorerOption
    {
        public string CryptoCode { get; internal set; }
        public Uri ExplorerUri { get; internal set; }
        public string CookieFile { get; internal set; }
    }
    public static class EthereumExtensions
    {
        private const string EthereumClientDbInfo = "for EthereumClient";

        public static IServiceCollection AddEthereumLike(this IServiceCollection services)
        {
            services.AddSingleton(s => s.ConfigureEthereumConfiguration());

            services.TryAddSingleton<EthereumExplorerClientProvider>();
            services.TryAddSingleton<EthereumWalletProvider>();
            services.TryAddSingleton<EthereumDashboard>();
            services.TryAddSingleton<EthereumUiUtilService>();
            services.AddSingleton<IHostedService, EthereumXplorerWaiters>();
            services.AddSingleton<IHostedService, EthereumXplorerListener>();

            return services;
        }

        public static EthereumOptions ConfigureEthereumConfiguration(this IServiceProvider serviceProvider)
        {
            BTCPayServerOptions bTCPayServerOptions = serviceProvider.GetService<BTCPayServerOptions>();
            IConfiguration conf = serviceProvider.GetService<IConfiguration>();
            EthereumOptions ethereumOptions = new EthereumOptions();
            foreach (var net in bTCPayServerOptions.NetworkProvider.GetAll().OfType<EthereumLikecBtcPayNetwork>())
            {
                EthereumNBXplorerOption setting = new EthereumNBXplorerOption();
                setting.CryptoCode = net.CryptoCode;
                setting.ExplorerUri = conf.GetOrDefault<Uri>($"{net.CryptoCode}.explorer.url", new Uri("http://localhost"));
                setting.CookieFile = conf.GetOrDefault<string>($"{net.CryptoCode}.explorer.cookiefile", "");
                ethereumOptions.EthereumNBXplorerOptions.Add(setting);
            }
            return ethereumOptions;
        }
    }
}
