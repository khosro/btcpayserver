using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Ethereum.ViewModels
{
    public class EthPaymentMethodViewModel
    {
        [Required]
        public string CryptoCode { get; set; }
        [Required]
        public string Mnemonic { get; set; }
        public bool Enabled { get; set; }
    }
}
