using System;
using System.IO;
using System.Linq;
using BTCPayServer.Configuration;
using BTCPayServer.Ethereum.Client;
using BTCPayServer.Ethereum.Config;
using BTCPayServer.Ethereum.HostedServices;
using BTCPayServer.Ethereum.Services.Wallet;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DatabaseType = BTCPayServer.Ethereum.Client.DatabaseType;
namespace BTCPayServer
{
    public static class EthereumExtensions
    {
        const string EthereumClientDbInfo = "for EthereumClient";

        public static IServiceCollection AddEthereumLike(this IServiceCollection services)
        {
            services.AddSingleton(s => s.ConfigureEthereumConfiguration());
            services.AddSingleton<IHostedService, EthereumListener>();
            services.TryAddSingleton<EthereumClientProvider>();
            services.TryAddSingleton<EthereumWalletProvider>();
            services.TryAddSingleton<EthereumDashboard>();
            services.AddSingleton<IHostedService, EthereumWaiters>();

            Database(services);

            services.AddStartupTask<EthereumDataMigrationStartupTask>();

            return services;
        }

        private static void Database(IServiceCollection services)
        {
            services.TryAddSingleton<EthereumClientApplicationDbContextFactory>(o =>
            {
                var opts = o.GetRequiredService<EthereumOptions>();
                EthereumClientApplicationDbContextFactory dbContext = null;
                if (!String.IsNullOrEmpty(opts.PostgresConnectionString))
                {
                    Logs.Configuration.LogInformation($"Postgres DB used ({opts.PostgresConnectionString}) {EthereumClientDbInfo}");
                    dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.Postgres, opts.PostgresConnectionString);
                }
                else if (!String.IsNullOrEmpty(opts.MySQLConnectionString))
                {
                    Logs.Configuration.LogInformation($"MySQL DB used ({opts.MySQLConnectionString}) {EthereumClientDbInfo}");
                    Logs.Configuration.LogWarning($"MySQL is not widely tested and should be considered experimental, we advise you to use postgres instead. {EthereumClientDbInfo}");
                    dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.MySQL, opts.MySQLConnectionString);
                }
                else
                {
                    var connStr = "Data Source=" + Path.Combine(opts.DataDir, "sqllite.db");
                    Logs.Configuration.LogInformation($"SQLite DB used ({connStr}) {EthereumClientDbInfo}");
                    Logs.Configuration.LogWarning($"SQLite not widely tested and should be considered experimental, we advise you to use postgres instead. {EthereumClientDbInfo}");
                    dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.Sqlite, connStr);
                }

                return dbContext;
            });

            services.AddDbContext<EthereumClientApplicationDbContext>((provider, o) =>
            {
                var factory = provider.GetRequiredService<EthereumClientApplicationDbContextFactory>();
                factory.ConfigureBuilder(o);
            });

            services.TryAddSingleton<EthereumClientTransactionRepository>();
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
                string wsurl = configuration.GetOrDefault<string>($"{net.CryptoCode}.eth.wsurl", null);

                if (rpcUri == null && string.IsNullOrWhiteSpace(wsurl))
                {
                    throw new ConfigException($"{net.CryptoCode} is misconfigured for rpc url({net.CryptoCode}.eth.rcpurl) and websocket url({net.CryptoCode}.eth.wsurl)");
                }

                result.EthereumConfigs.Add(new EthereumConfig { RpcUri = rpcUri, CryptoCode = net.CryptoCode, WebsocketUrl = wsurl });
            }

            var dataDir = StandardConfiguration.DefaultDataDirectory.GetDirectory("BTCPayServer", "EthClient");

            result.DataDir = dataDir;
            result.PostgresConnectionString = configuration.GetOrDefault<string>("eth.postgres", null);
            result.MySQLConnectionString = configuration.GetOrDefault<string>("eth.mysql", null);

            if (string.IsNullOrWhiteSpace(result.MySQLConnectionString) && string.IsNullOrWhiteSpace(result.PostgresConnectionString))
            {
                throw new ConfigException($"Please provide MySQLConnectionString or PostgresConnectionString {EthereumClientDbInfo} ");
            }

            return result;
        }
    }
}
