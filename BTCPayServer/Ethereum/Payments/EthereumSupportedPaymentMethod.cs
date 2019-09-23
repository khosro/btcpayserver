using BTCPayServer.Payments;

namespace BTCPayServer.Ethereum.Payments
{
    public class EthereumSupportedPaymentMethod : ISupportedPaymentMethod
    {
        public string CryptoCode { get; set; }
        public string Mnemonic { get; set; }
        public PaymentMethodId PaymentId => new PaymentMethodId(CryptoCode, EthereumPaymentType.Instance);
    }
}
