//using System;
//using System.Collections.Generic;
//using System.Linq;
//using BTCPayServer.Ethereum.Client;
//using BTCPayServer.Ethereum.Config;
//using BTCPayServer.HostedServices;
//using BTCPayServer.Logging;
//using Microsoft.Extensions.Logging;
//using static BTCPayServer.HostedServices.EthereumDashboard;

//namespace BTCPayServer
//{
//    public class EthereumClientProvider
//    {
//        private BTCPayNetworkProvider _NetworkProviders;
//        private EthereumOptions _EthereumOptions;
//        private Dictionary<string, EthereumClient> _Clients = new Dictionary<string, EthereumClient>();
//        EthereumClientTransactionRepository _ethereumClientTransactionRepository;
//        public BTCPayNetworkProvider NetworkProviders => _NetworkProviders;

//        private EthereumDashboard _Dashboard;
//        public EthereumClientProvider(BTCPayNetworkProvider networkProviders, EthereumOptions ethereumOptions, EthereumDashboard dashboard,
//             EthereumClientTransactionRepository ethereumClientTransactionRepository)
//        {
//            _Dashboard = dashboard;
//            _NetworkProviders = networkProviders;
//            _EthereumOptions = ethereumOptions;
//            this._ethereumClientTransactionRepository = ethereumClientTransactionRepository;
//            foreach (EthereumConfig setting in _EthereumOptions.EthereumConfigs)
//            {
//                Logs.Configuration.LogInformation($"{setting.CryptoCode}:  Ethereum url is {(setting.RpcUri.AbsoluteUri ?? "not set")}");
//                if (setting.RpcUri != null)
//                {
//                    _Clients.TryAdd(setting.CryptoCode, CreateEthereumClient(_NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(setting.CryptoCode), setting));
//                }
//            }
//        }

//        private EthereumClient CreateEthereumClient(EthereumLikecBtcPayNetwork n, EthereumConfig setting)
//        {
//            var client = new EthereumClient(setting.RpcUri, setting.WebsocketUrl, n, _NetworkProviders, _ethereumClientTransactionRepository);
//            return client;
//        }

//        public EthereumClient GetEthereumClient(string cryptoCode)
//        {
//            EthereumLikecBtcPayNetwork network = _NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(cryptoCode);
//            if (network == null)
//            {
//                return null;
//            }

//            _Clients.TryGetValue(network.CryptoCode, out EthereumClient client);
//            return client;
//        }

//        public EthereumClient GetEthereumClient(EthereumLikecBtcPayNetwork network)
//        {
//            if (network == null)
//            {
//                throw new ArgumentNullException(nameof(network));
//            }

//            return GetEthereumClient(network.CryptoCode);
//        }

//        public bool IsAvailable(EthereumLikecBtcPayNetwork network)
//        {
//            return IsAvailable(network.CryptoCode);
//        }

//        public bool IsAvailable(string cryptoCode)
//        {
//            return _Clients.ContainsKey(cryptoCode) && _Dashboard.IsFullySynched(cryptoCode, out EthereumSummary unused);
//        }

//        public EthereumLikecBtcPayNetwork GetNetwork(string cryptoCode)
//        {
//            EthereumLikecBtcPayNetwork network = _NetworkProviders.GetNetwork<EthereumLikecBtcPayNetwork>(cryptoCode);
//            if (network == null)
//            {
//                return null;
//            }

//            if (_Clients.ContainsKey(network.CryptoCode))
//            {
//                return network;
//            }

//            return null;
//        }

//        public IEnumerable<(EthereumLikecBtcPayNetwork, EthereumClient)> GetAll()
//        {
//            foreach (EthereumLikecBtcPayNetwork net in _NetworkProviders.GetAll().OfType<EthereumLikecBtcPayNetwork>())
//            {
//                if (_Clients.TryGetValue(net.CryptoCode, out EthereumClient client))
//                {
//                    yield return (net, client);
//                }
//            }
//        }
//    }
//}
