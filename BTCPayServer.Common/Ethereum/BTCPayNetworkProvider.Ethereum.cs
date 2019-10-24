using NBitcoin;
namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitEthereum()
        {
            NBXplorer.NBXplorerNetwork nbxplorerNetwork = NBXplorerNetworkProvider.GetFromCryptoCode("ETH");

            Add(new EthereumLikecBtcPayNetwork()
            {
                CryptoCode = "ETH",
                DisplayName = "Ethereum",
                BlockExplorerLink =
                    NetworkType == NetworkType.Mainnet
                        ? "https://etherscan.io/tx/{0}"
                        : "https://ropsten.etherscan.io/tx/{0}",
                CryptoImagePath = "/imlegacy/ethereum.png",
                NBXplorerNetwork = nbxplorerNetwork,
                /*
                 * If EthereumLikecBtcPayNetwork inherit from BTCPayNetwork, then we can use them
                NBitcoinNetwork = nbxplorerNetwork.NBitcoinNetwork,
                CoinType = new KeyPath("60'")*/
            });
        }
    }
}

