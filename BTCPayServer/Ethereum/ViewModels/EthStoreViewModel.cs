using System.Collections.Generic;

namespace BTCPayServer.Ethereum.ViewModels
{
    public class EthStoreViewModel
    {
        public EthStoreViewModel()
        {
            EthPaymentMethodViewModels = new List<EthPaymentMethodViewModel>();
        }
        public IEnumerable<EthPaymentMethodViewModel> EthPaymentMethodViewModels { get; set; }
        public string StatusMessage { get; set; }
    }
}
