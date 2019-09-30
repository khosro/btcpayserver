using Newtonsoft.Json;

namespace BTCPayServer.Altcoins.Monero.RPC.Models
{
    public class GetFeeEstimateResponse
    {
        [JsonProperty("fee")] public long Fee { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("untrusted")] public bool Untrusted { get; set; }
    }
}
