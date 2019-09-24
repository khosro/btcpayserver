using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Payments;
using BTCPayServer.Ethereum.Services.Wallet;
using BTCPayServer.HostedServices;
using BTCPayServer.ModelBinders;
using BTCPayServer.Models.WalletViewModels;
using BTCPayServer.Security;
using BTCPayServer.Services;
using BTCPayServer.Services.Rates;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Controllers
{
    public partial class WalletsController : Controller
    {
        EthereumWalletProvider _ethereumWalletProvider;
        public WalletsController(StoreRepository repo,
                                       WalletRepository walletRepository,
                                       CurrencyNameTable currencyTable,
                                       BTCPayNetworkProvider networkProvider,
                                       UserManager<ApplicationUser> userManager,
                                       IOptions<MvcJsonOptions> mvcJsonOptions,
                                       NBXplorerDashboard dashboard,
                                       RateFetcher rateProvider,
                                       ExplorerClientProvider explorerProvider,
                                       IFeeProviderFactory feeRateProvider,
                                       BTCPayWalletProvider walletProvider, EthereumWalletProvider ethereumWalletProvider) :
            this(repo, walletRepository, currencyTable, networkProvider, userManager, mvcJsonOptions, dashboard, rateProvider, explorerProvider, feeRateProvider, walletProvider)
        {
            this._ethereumWalletProvider = ethereumWalletProvider;
        }

        [HttpGet]
        [Route("ethwallet/{walletId}")]
        public async Task<IActionResult> EthWalletTransactions([ModelBinder(typeof(WalletIdModelBinder))]  WalletId walletId, string labelFilter = null)
        {
            DerivationSchemeSettings paymentMethod = await GetDerivationSchemeSettings(walletId);
            if (paymentMethod == null)
                return NotFound();

            var wallet = _ethereumWalletProvider.GetWallet(paymentMethod.Network);
            var walletBlobAsync = WalletRepository.GetWalletInfo(walletId);
            var walletTransactionsInfoAsync = WalletRepository.GetWalletTransactionsInfo(walletId);
            var transactions = await wallet.FetchTransactions(await GetEthPatymentMethod(walletId));
            var walletBlob = await walletBlobAsync;
            var walletTransactionsInfo = await walletTransactionsInfoAsync;
            var model = new ListTransactionsViewModel();
            foreach (var tx in transactions)
            {
                var vm = new ListTransactionsViewModel.TransactionViewModel();
                vm.Id = tx.TransactionHash;
                vm.Link = string.Format(CultureInfo.InvariantCulture, paymentMethod.Network.BlockExplorerLink, vm.Id);
                vm.Positive = tx.Value.Value > 0;
                vm.Balance = tx.Value.Value.ToString();
                //vm.IsConfirmed = tx. != 0;
                //vm.Timestamp = tx.Input.;

                if (walletTransactionsInfo.TryGetValue(tx.TransactionHash, out var transactionInfo))
                {
                    var labels = walletBlob.GetLabels(transactionInfo);
                    vm.Labels.AddRange(labels);
                    model.Labels.AddRange(labels);
                    vm.Comment = transactionInfo.Comment;
                }

                if (labelFilter == null || vm.Labels.Any(l => l.Value.Equals(labelFilter, StringComparison.OrdinalIgnoreCase)))
                    model.Transactions.Add(vm);
            }
            model.Transactions = model.Transactions.OrderByDescending(t => t.Timestamp).ToList();
            return View(model);
        }

        private async Task<EthereumSupportedPaymentMethod> GetEthPatymentMethod(WalletId walletId)
        {
            var store = (await Repository.FindStore(walletId.StoreId, GetUserId()));
            if (store == null || !store.HasClaim(Policies.CanModifyStoreSettings.Key))
                return null;

            var paymentMethod = store.GetSupportedPaymentMethods(NetworkProvider).OfType<EthereumSupportedPaymentMethod>()
                            .FirstOrDefault(p => p.PaymentId.PaymentType == Payments.PaymentTypes.BTCLike && p.PaymentId.CryptoCode == walletId.CryptoCode);
            return paymentMethod;
        }


    }
}
