using System;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.Ethereum.ViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Security;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BTCPayServer.Controllers
{
    [Authorize(AuthenticationSchemes = Policies.CookieAuthentication)]
    [Authorize(Policy = Policies.CanModifyStoreSettings.Key, AuthenticationSchemes = Policies.CookieAuthentication)]
    [Authorize(Policy = Policies.CanModifyServerSettings.Key, AuthenticationSchemes = Policies.CookieAuthentication)]
    public partial class StoresController : Controller
    {
        [Route("{storeId}/ethstore/{cryptoCode}")]
        public IActionResult GetEthPaymentMethod(string cryptoCode, string statusMessage = null)
        {
            BTCPayNetworkProvider _BtcPayNetworkProvider = (BTCPayNetworkProvider)_ServiceProvider.GetService(typeof(BTCPayNetworkProvider));
            System.Collections.Generic.IEnumerable<string> cryptoCodes = _BtcPayNetworkProvider.GetEthCryptoCodes();
            cryptoCode = cryptoCode.ToUpperInvariant();
            EthereumSupportedPaymentMethod ethereumSupportedPaymentMethods = StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider).OfType<EthereumSupportedPaymentMethod>().SingleOrDefault(t => t.CryptoCode.ToUpperInvariant().Equals(cryptoCode));

            IPaymentFilter excludeFilters = StoreData.GetStoreBlob().GetExcludedPaymentMethods();

            if (!cryptoCodes.Any(t => !t.Equals(cryptoCode, StringComparison.InvariantCulture)))
            {
                return NotFound();
            }
            return View("~/Views/EthereumStore/GetEthPaymentMethod.cshtml", new EthPaymentMethodViewModel()
            {
                CryptoCode = cryptoCode,
                Mnemonic = ethereumSupportedPaymentMethods != null ? ethereumSupportedPaymentMethods.Mnemonic : "",
                Enabled = ethereumSupportedPaymentMethods != null ? !excludeFilters.Match(ethereumSupportedPaymentMethods.PaymentId) : false,
            });
        }

        [Route("{storeId}/ethstore/{cryptoCode}")]
        [HttpPost]
        public async Task<IActionResult> GetEthPaymentMethod(EthPaymentMethodViewModel viewModel, string command, string cryptoCode)
        {
            StoreRepository _StoreRepository = (StoreRepository)_ServiceProvider.GetService(typeof(StoreRepository));

            PaymentMethodId paymentMethodId = new PaymentMethodId(cryptoCode, PaymentTypes.EthLike);

            if (!ModelState.IsValid)
            {
                var vm = new EthPaymentMethodViewModel() { CryptoCode = cryptoCode };
                vm.Enabled = viewModel.Enabled;
                vm.Mnemonic = viewModel.Mnemonic;
                return View(vm);
            }
            StoreData storeData = StoreData;
            StoreBlob blob = storeData.GetStoreBlob();

            StoreData.SetSupportedPaymentMethod(new EthereumSupportedPaymentMethod() { CryptoCode = cryptoCode, Mnemonic = viewModel.Mnemonic });

            blob.SetExcluded(new PaymentMethodId(viewModel.CryptoCode, EthereumPaymentType.Instance), !viewModel.Enabled);
            storeData.SetStoreBlob(blob);
            await _StoreRepository.UpdateStore(storeData);
            return RedirectToAction("UpdateStore", "Stores", new
            {
                storeId = StoreData.Id
            });
        }
    }
}
