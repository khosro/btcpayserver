using System;
using System.Linq;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.Ethereum.ViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Mvc;
namespace BTCPayServer.Ethereum.UiUtil
{
    [ViewComponent(Name = "EthereumPayments")]
    public class EthereumPaymentssViewComponent : ViewComponent
    {
        private readonly StoreRepository _StoreRepository;
        private readonly BTCPayNetworkProvider _BtcPayNetworkProvider;

        public EthereumPaymentssViewComponent(StoreRepository storeRepository, BTCPayNetworkProvider btcPayNetworkProvider)
        {
            _StoreRepository = storeRepository;
            _BtcPayNetworkProvider = btcPayNetworkProvider;
        }

        public StoreData StoreData => HttpContext.GetStoreData();

        public IViewComponentResult Invoke()
        {
            System.Collections.Generic.IEnumerable<EthereumSupportedPaymentMethod> ethereumSupportedPaymentMethods = StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider).OfType<EthereumSupportedPaymentMethod>();

            IPaymentFilter excludeFilters = StoreData.GetStoreBlob().GetExcludedPaymentMethods();

            System.Collections.Generic.IEnumerable<string> ethCryptoCodes = _BtcPayNetworkProvider.GetEthNetworks().Select(t => t.CryptoCode);

            return View(new EthStoreViewModel()
            {
                EthPaymentMethodViewModels = ethCryptoCodes.Select(cryptoCode =>
                FillEthPaymentMethodViewModel(ethereumSupportedPaymentMethods.SingleOrDefault(t => t.CryptoCode.Equals(cryptoCode, StringComparison.InvariantCulture)), cryptoCode, excludeFilters))
            });
        }

        private EthPaymentMethodViewModel FillEthPaymentMethodViewModel(EthereumSupportedPaymentMethod ethereumSupportedPaymentMethod, string cryptoCode, IPaymentFilter excludeFilters)
        {
            return new EthPaymentMethodViewModel()
            {
                CryptoCode = cryptoCode,
                Enabled = ethereumSupportedPaymentMethod != null ? !excludeFilters.Match(ethereumSupportedPaymentMethod.PaymentId) : false,
                Mnemonic = ethereumSupportedPaymentMethod != null ? ethereumSupportedPaymentMethod.Mnemonic : "",
                WalletId = ethereumSupportedPaymentMethod != null ? new WalletId(StoreData.Id, ethereumSupportedPaymentMethod.CryptoCode) : null
            };
        }
    }
}
