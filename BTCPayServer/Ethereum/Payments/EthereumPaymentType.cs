using System;
using System.Globalization;
using BTCPayServer.Payments;
using BTCPayServer.Services.Invoices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Ethereum.Payments
{

    public class EthereumPaymentType : PaymentType
    {
        public static EthereumPaymentType Instance { get; } = new EthereumPaymentType();
        public override string ToPrettyString()
        {
            return "";
        }

        public override string GetId() => "ethlike";


        public override CryptoPaymentData DeserializePaymentData(string str)
        {

#pragma warning disable CS0618
            return JsonConvert.DeserializeObject<EthereumLikePaymentData>(str);
#pragma warning restore CS0618
        }

        public override IPaymentMethodDetails DeserializePaymentMethodDetails(string str)
        {
            return JsonConvert.DeserializeObject<EthereumPaymentMethodDetails>(str);
        }

        public override ISupportedPaymentMethod DeserializeSupportedPaymentMethod(BTCPayNetworkBase network, JToken value)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is JObject jobj)
            {
                EthereumSupportedPaymentMethod scheme = JsonConvert.DeserializeObject<EthereumSupportedPaymentMethod>(value.ToString());
                scheme.Network = network;
                return scheme;
            }
            // Legacy
            return null;
        }

        public override string GetTransactionLink(BTCPayNetworkBase network, string txId)
        {
            return string.Format(CultureInfo.InvariantCulture, network.BlockExplorerLink, txId);
        }

        public override string InvoiceViewPaymentPartialName { get; } = "Eth/EthPaymentData";
    }
}
