using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using BTCPayServer.Configuration;
using BTCPayServer.HostedServices;
using BTCPayServer.Logging;
using EthereumXplorer.Client;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Ethereum
{
    public class EthereumExplorerClientProvider
    {
        private BTCPayNetworkProvider _NetworkProviders;
        private readonly BTCPayServerOptions _Options;

        public BTCPayNetworkProvider NetworkProviders => _NetworkProviders;

        private EthereumDashboard _Dashboard;
        public EthereumExplorerClientProvider(DummyEthereumOptions dummyEthereumOptions, IHttpClientFactory httpClientFactory, BTCPayNetworkProvider networkProviders, BTCPayServerOptions options, EthereumDashboard dashboard)
        {
            _Dashboard = dashboard;
            _NetworkProviders = networkProviders;
            _Options = options;
            IEnumerable<EthereumLikecBtcPayNetwork> ethNetworks = networkProviders.GetAll().OfType<EthereumLikecBtcPayNetwork>();
            foreach (NBXplorerConnectionSetting setting in options.NBXplorerConnectionSettings)
            {
                EthereumLikecBtcPayNetwork ethNetwork = ethNetworks.SingleOrDefault(t => t.CryptoCode.Equals(setting.CryptoCode, StringComparison.InvariantCulture));
                if (ethNetwork == null)
                {
                    continue;
                }

                var cookieFile = setting.CookieFile;
                if (cookieFile.Trim() == "0" || string.IsNullOrEmpty(cookieFile.Trim()))
                {
                    cookieFile = null;
                }

                Logs.Configuration.LogInformation($"{setting.CryptoCode}: Explorer url is {(setting.ExplorerUri.AbsoluteUri ?? "not set")}");
                Logs.Configuration.LogInformation($"{setting.CryptoCode}: Cookie file is {(setting.CookieFile ?? "not set")}");
                if (setting.ExplorerUri != null)
                {
                    _Clients.TryAdd(setting.CryptoCode, CreateExplorerClient(httpClientFactory.CreateClient(nameof(ExplorerClientProvider)),
                        _NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(setting.CryptoCode), setting.ExplorerUri, setting.CookieFile));
                }
            }
        }

        private static EthereumExplorerClient CreateExplorerClient(HttpClient httpClient, EthereumLikecBtcPayNetwork n, Uri uri, string cookieFile)
        {
            var explorer = new EthereumExplorerClient(n.NBXplorerNetwork, uri);
            explorer.SetClient(httpClient);
            if (cookieFile == null)
            {
                Logs.Configuration.LogWarning($"{n.CryptoCode}: Not using cookie authentication");
                explorer.SetNoAuth();
            }
            if (!explorer.SetCookieAuth(cookieFile))
            {
                Logs.Configuration.LogWarning($"{n.CryptoCode}: Using cookie auth against NBXplorer, but {cookieFile} is not found");
            }
            return explorer;
        }

        private Dictionary<string, EthereumExplorerClient> _Clients = new Dictionary<string, EthereumExplorerClient>();

        public EthereumExplorerClient GetExplorerClient(string cryptoCode)
        {
            EthereumLikecBtcPayNetwork network = _NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(cryptoCode);
            if (network == null)
            {
                return null;
            }

            _Clients.TryGetValue(network.CryptoCode, out EthereumExplorerClient client);
            return client;
        }

        public EthereumExplorerClient GetExplorerClient(BTCPayNetworkBase network)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            return GetExplorerClient(network.CryptoCode);
        }

        public bool IsAvailable(BTCPayNetworkBase network)
        {
            return IsAvailable(network.CryptoCode);
        }

        public bool IsAvailable(string cryptoCode)
        {
            return _Clients.ContainsKey(cryptoCode) && _Dashboard.IsFullySynched(cryptoCode, out EthereumDashboard.EthereumSummary unused);
        }

        public EthereumLikecBtcPayNetwork GetNetwork(string cryptoCode)
        {
            EthereumLikecBtcPayNetwork network = _NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(cryptoCode);
            if (network == null)
            {
                return null;
            }

            if (_Clients.ContainsKey(network.CryptoCode))
            {
                return network;
            }

            return null;
        }

        public IEnumerable<(EthereumLikecBtcPayNetwork, EthereumExplorerClient)> GetAll()
        {
            foreach (EthereumLikecBtcPayNetwork net in _NetworkProviders.GetAll().OfType<EthereumLikecBtcPayNetwork>())
            {
                if (_Clients.TryGetValue(net.CryptoCode, out EthereumExplorerClient explorer))
                {
                    yield return (net, explorer);
                }
            }
        }
    }
}
