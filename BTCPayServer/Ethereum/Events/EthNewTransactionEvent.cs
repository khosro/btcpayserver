using BTCPayServer.Ethereum.Services.Wallet;
using Nethereum.RPC.Eth.DTOs;
namespace BTCPayServer.Ethereum.Events
{
    public class EthNewTransactionEvent
    {
        public EthNewTransactionEvent(EthereumWallet ethereumWallet, Transaction transaction)
        {
            EthereumWallet = ethereumWallet;
            Transaction = transaction;
        }

        public EthereumWallet EthereumWallet { get; set; }
        public Transaction Transaction { get; set; }
    }
}
