using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCPayServer
{

    public static class EthereumExtensions
    {
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
