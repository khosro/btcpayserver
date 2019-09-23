using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Config;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.Ethereum.ViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Mvc;
namespace BTCPayServer.Ethereum.UiUtil
{
    [ViewComponent(Name = "EthereumPayment")]
    public class EthereumPaymentViewComponent : ViewComponent
    {

        private readonly EthereumOptions _ethereumOptions;
        private readonly StoreRepository _StoreRepository;
        private readonly BTCPayNetworkProvider _BtcPayNetworkProvider;

        public EthereumPaymentViewComponent(EthereumOptions ethereumOptions, StoreRepository storeRepository, BTCPayNetworkProvider btcPayNetworkProvider)
        {
            _ethereumOptions = ethereumOptions;
            _StoreRepository = storeRepository;
            _BtcPayNetworkProvider = btcPayNetworkProvider;
        }

        public StoreData StoreData => HttpContext.GetStoreData();

        public IViewComponentResult Invoke()
        {
            var ethereumSupportedPaymentMethods = StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider).OfType<EthereumSupportedPaymentMethod>();

            var excludeFilters = StoreData.GetStoreBlob().GetExcludedPaymentMethods();

            var ethCryptoCodes = _ethereumOptions.EthereumConfigs.Select(t => t.CryptoCode);

            return View(new EthStoreViewModel()
            {
                EthPaymentMethodViewModels = ethCryptoCodes.Select(cryptoCode =>
                FillEthPaymentMethodViewModel(ethereumSupportedPaymentMethods.SingleOrDefault(t => t.CryptoCode.Equals(cryptoCode)), cryptoCode, excludeFilters))
            });
        }
         

        EthPaymentMethodViewModel FillEthPaymentMethodViewModel(EthereumSupportedPaymentMethod ethereumSupportedPaymentMethod, string cryptoCode, IPaymentFilter excludeFilters)
        {
            return new EthPaymentMethodViewModel()
            {
                CryptoCode = cryptoCode,
                Enabled = ethereumSupportedPaymentMethod != null ? !excludeFilters.Match(ethereumSupportedPaymentMethod.PaymentId) : false,
                Mnemonic = ethereumSupportedPaymentMethod != null ? ethereumSupportedPaymentMethod.Mnemonic : ""

            };
        }
    }
}
