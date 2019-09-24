using BTCPayServer.Payments;
using Newtonsoft.Json;

namespace BTCPayServer.Ethereum.Payments
{
    public class EthereumSupportedPaymentMethod : ISupportedPaymentMethod
    {
        [JsonIgnore]
        public BTCPayNetworkBase Network { get; set; }
        private string _mnemonic;
        public string CryptoCode { get; set; }
        public string Mnemonic { get { return _mnemonic; } set { _mnemonic = value?.Trim(); } }
        public PaymentMethodId PaymentId => new PaymentMethodId(CryptoCode, EthereumPaymentType.Instance);
    }
}
