using System.Numerics;
using BTCPayServer.Ethereum.Services.Wallet;

namespace BTCPayServer.Ethereum.Events
{
    public class EthNewBlock
    {
        public EthNewBlock(BigInteger blockNumber, EthereumWallet ethereumWallet)
        {
            BlockNumber = blockNumber;
            EthereumWallet = ethereumWallet;
        }

        public BigInteger BlockNumber { get; }
        public EthereumWallet EthereumWallet { get; }
    }
}
