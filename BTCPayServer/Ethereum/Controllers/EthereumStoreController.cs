using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Config;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.Security;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BTCPayServer.Controllers
{
    [Route("stores/{storeId}/eth")]
    [Authorize(AuthenticationSchemes = Policies.CookieAuthentication)]
    [Authorize(Policy = Policies.CanModifyStoreSettings.Key, AuthenticationSchemes = Policies.CookieAuthentication)]
    [Authorize(Policy = Policies.CanModifyServerSettings.Key, AuthenticationSchemes = Policies.CookieAuthentication)]
    public class EthereumStoreController : Controller
    {
        private readonly EthereumOptions _ethereumOptions;
        private readonly StoreRepository _StoreRepository;
        private readonly BTCPayNetworkProvider _BtcPayNetworkProvider;

        public EthereumStoreController(EthereumOptions ethereumOptions,
             StoreRepository storeRepository,
             BTCPayNetworkProvider btcPayNetworkProvider)
        {
            _ethereumOptions = ethereumOptions;
            _StoreRepository = storeRepository;
            _BtcPayNetworkProvider = btcPayNetworkProvider;
        }

        public StoreData StoreData => HttpContext.GetStoreData();

        [HttpGet()]
        public async Task<IActionResult> GetEthPaymentMethods(string statusMessage)
        {
            var ethereumSupportedPaymentMethods = StoreData.GetSupportedPaymentMethods(_BtcPayNetworkProvider)
                .OfType<EthereumSupportedPaymentMethod>();

            var excludeFilters = StoreData.GetStoreBlob().GetExcludedPaymentMethods();

            var ethCryptoCode = _ethereumOptions.EthereumConfigs.Select(t => t.CryptoCode);

            return null;
        }
    }
}
