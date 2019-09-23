using BTCPayServer.Payments;

namespace BTCPayServer.Ethereum.Payments
{
    public class EthereumPaymentMethodDetails : IPaymentMethodDetails
    {
        public PaymentType GetPaymentType()
        {
            return EthereumPaymentType.Instance;
        }

        public string GetPaymentDestination()
        {
            return DepositAddress;
        }

        public decimal GetNextNetworkFee()
        {
            return NextNetworkFee;
        }
        public void SetPaymentDestination(string newPaymentDestination)
        {
            DepositAddress = newPaymentDestination;
        }
        public string DepositAddress { get; set; }
        public decimal NextNetworkFee { get; set; }
    }


}
