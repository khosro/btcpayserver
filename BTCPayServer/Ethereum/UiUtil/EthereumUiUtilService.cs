using System.Linq;
namespace BTCPayServer.Ethereum.UiUtil
{
    public class EthereumUiUtilService
    {
        private readonly BTCPayNetworkProvider _bTCPayNetworkProvider;
        public EthereumUiUtilService(BTCPayNetworkProvider bTCPayNetworkProvider)
        {
            _bTCPayNetworkProvider = bTCPayNetworkProvider;
        }

        public bool IsEthNetworkProvides()
        {
            return _bTCPayNetworkProvider.GetEthCryptoCodes().Any();
        }
    }
}
