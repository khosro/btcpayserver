namespace BTCPayServer
{
    public class EthereumLikecBtcPayNetwork : BTCPayNetworkBase
    {
        public long MaxTrackedConfirmation { get; set; }
        public NBXplorer.NBXplorerNetwork NBXplorerNetwork { get; set; }
    }
}
