﻿using BTCPayServer.Data;
using BTCPayServer.Payments;
using BTCPayServer.Services.Invoices;
namespace BTCPayServer.Ethereum.Payments
{
    public class EthereumLikePaymentData : CryptoPaymentData
    {
        public long Amount { get; set; }
        public string Address { get; set; }
        public long BlockHeight { get; set; }
        public long ConfirmationCount { get; set; }
        public string TransactionId { get; set; }

        public BTCPayNetworkBase Network { get; set; }

        public string GetPaymentId()
        {
            return $"{TransactionId}";
        }

        public string[] GetSearchTerms()
        {
            return new[] { TransactionId };
        }

        //TODO.It must be changed.
        public decimal GetValue()
        {
            return 0;
        }

        public bool PaymentCompleted(PaymentEntity entity)
        {
            return ConfirmationCount >= (Network as EthereumLikecBtcPayNetwork).MaxTrackedConfirmation;
        }

        public bool PaymentConfirmed(PaymentEntity entity, SpeedPolicy speedPolicy)
        {
            switch (speedPolicy)
            {
                case SpeedPolicy.HighSpeed:
                    return ConfirmationCount >= 0;
                case SpeedPolicy.MediumSpeed:
                    return ConfirmationCount >= 1;
                case SpeedPolicy.LowMediumSpeed:
                    return ConfirmationCount >= 2;
                case SpeedPolicy.LowSpeed:
                    return ConfirmationCount >= 6;
                default:
                    return false;
            }
        }

        public PaymentType GetPaymentType()
        {
            return EthereumPaymentType.Instance;
        }

        public string GetDestination()
        {
            return Address;
        }
    }
}
