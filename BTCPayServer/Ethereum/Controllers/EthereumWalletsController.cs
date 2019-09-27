using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Ethereum.Model;
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
using EthereumXplorer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Controllers
{
    public partial class WalletsController : Controller
    {
        private BTCPayNetworkProvider _networkProvider;
        private EthereumWalletProvider _ethereumWalletProvider;
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
            _ethereumWalletProvider = ethereumWalletProvider;
            _networkProvider = networkProvider;
        }

        [HttpGet]
        [Route("ethwallet/{walletId}")]
        public async Task<IActionResult> EthWalletTransactions([ModelBinder(typeof(WalletIdModelBinder))]  WalletId walletId, string labelFilter = null)
        {
            EthereumSupportedPaymentMethod paymentMethod = await GetEthPatymentMethod(walletId);
            if (paymentMethod == null)
            {
                return NotFound();
            }
            EthereumWallet wallet = _ethereumWalletProvider.GetWallet(paymentMethod.Network);
            Task<WalletBlobInfo> walletBlobAsync = WalletRepository.GetWalletInfo(walletId);
            Task<Dictionary<string, WalletTransactionInfo>> walletTransactionsInfoAsync = WalletRepository.GetWalletTransactionsInfo(walletId);
            IEnumerable<EthereumClientTransactionData> transactions = await wallet.GetTransactionsAsync(paymentMethod.Mnemonic);
            WalletBlobInfo walletBlob = await walletBlobAsync;
            Dictionary<string, WalletTransactionInfo> walletTransactionsInfo = await walletTransactionsInfoAsync;
            var model = new ListTransactionsViewModel();
            foreach (var tx in transactions)
            {
                var vm = new ListTransactionsViewModel.TransactionViewModel
                {
                    Id = tx.TransactionHash
                };
                vm.Link = string.Format(CultureInfo.InvariantCulture, paymentMethod.Network.BlockExplorerLink, vm.Id);
                vm.Positive = tx.Amount > 0;
                vm.Balance = tx.Amount.ToString();
                vm.Timestamp = tx.CreatedDateTime;
                //vm.IsConfirmed = tx. != 0;

                if (walletTransactionsInfo.TryGetValue(tx.TransactionHash, out WalletTransactionInfo transactionInfo))
                {
                    System.Collections.Generic.IEnumerable<Label> labels = walletBlob.GetLabels(transactionInfo);
                    vm.Labels.AddRange(labels);
                    model.Labels.AddRange(labels);
                    vm.Comment = transactionInfo.Comment;
                }

                if (labelFilter == null || vm.Labels.Any(l => l.Value.Equals(labelFilter, StringComparison.OrdinalIgnoreCase)))
                {
                    model.Transactions.Add(vm);
                }
            }
            model.Transactions = model.Transactions.OrderByDescending(t => t.Timestamp).ToList();
            return View(model);
        }

        [HttpGet]
        [Route("ethwallet/{walletId}/send")]
        public async Task<IActionResult> EthWalletSend([ModelBinder(typeof(WalletIdModelBinder))] WalletId walletId)
        {
            if (walletId?.StoreId == null)
                return NotFound();
            var store = await Repository.FindStore(walletId.StoreId, GetUserId());
            var paymentMethod = await GetEthPatymentMethod(walletId);
            if (paymentMethod == null)
                return NotFound();
            var network = this.NetworkProvider.GetNetwork<BTCPayNetworkBase>(walletId?.CryptoCode);
            if (network == null)
                return NotFound();
            var storeData = store.GetStoreBlob();

            EthereumWallet wallet = _ethereumWalletProvider.GetWallet(paymentMethod.Network);
            return View(await FillModel(wallet, paymentMethod));
        }

        [HttpPost]
        [Route("ethwallet/{walletId}/send")]
        public async Task<IActionResult> EthWalletSend(
            [ModelBinder(typeof(WalletIdModelBinder))]
            WalletId walletId, EthWalletSendModel vm, string command = "", CancellationToken cancellation = default)
        {
            var paymentMethod = await GetEthPatymentMethod(walletId);
            EthereumWallet wallet = _ethereumWalletProvider.GetWallet(paymentMethod.Network);
            try
            {
                string transhash = await wallet.BroadcastAsync(vm, paymentMethod.Mnemonic);
                var network = NetworkProvider.GetNetwork<BTCPayNetwork>(walletId.CryptoCode);
                StatusMessage = $"Transaction broadcasted successfully ({transhash})";
                return RedirectToAction(nameof(EthWalletTransactions));
            }
            catch (Exception ex)
            {
                vm = await FillModel(wallet, paymentMethod);
                vm.Error = "Unknown Error" + ex.Message;
                return View(vm);
            }
        }

        async Task<EthWalletSendModel> FillModel(EthereumWallet wallet, EthereumSupportedPaymentMethod paymentMethod)
        {
            var address2Balance = await wallet.GetBalanceByMnemonic(paymentMethod.Mnemonic);
            var model = new EthWalletSendModel();
            model.InitAddress(address2Balance);
            return model;
        }

        private async Task<EthereumSupportedPaymentMethod> GetEthPatymentMethod(WalletId walletId)
        {
            StoreData store = (await Repository.FindStore(walletId.StoreId, GetUserId()));
            if (store == null || !store.HasClaim(Policies.CanModifyStoreSettings.Key))
            {
                return null;
            }

            EthereumSupportedPaymentMethod paymentMethod = store.GetSupportedPaymentMethods(NetworkProvider).OfType<EthereumSupportedPaymentMethod>()
                            .FirstOrDefault(p => p.PaymentId.PaymentType == Payments.PaymentTypes.EthLike && p.PaymentId.CryptoCode == walletId.CryptoCode);
            return paymentMethod;
        }

    }
}
