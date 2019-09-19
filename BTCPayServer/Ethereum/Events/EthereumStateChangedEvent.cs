using BTCPayServer.HostedServices;

namespace BTCPayServer.Ethereum.Events
{
    public class EthereumStateChangedEvent
    {
        public EthereumStateChangedEvent(EthereumLikecBtcPayNetwork network, EthereumState old, EthereumState newState)
        {
            Network = network;
            NewState = newState;
            OldState = old;
        }

        public EthereumLikecBtcPayNetwork Network { get; set; }
        public EthereumState NewState { get; set; }
        public EthereumState OldState { get; set; }

        public override string ToString()
        {
            return $"EthereumState {Network.CryptoCode}: {OldState} => {NewState}";
        }
    }
}
