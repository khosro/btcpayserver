using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Ethereum.ViewModels
{
    public class EthPaymentMethodViewModel
    {
        private string _mnemonic;
        [Required]
        public string CryptoCode { get; set; }
        [Required]
        public string Mnemonic { get { return _mnemonic; } set { _mnemonic = value?.Trim(); } }
        public bool Enabled { get; set; }
        public WalletId WalletId { get; set; }
    }
}
