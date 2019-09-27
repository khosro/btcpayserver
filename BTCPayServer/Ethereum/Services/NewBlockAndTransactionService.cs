//using System;
//using System.Numerics;
//using System.Threading;
//using System.Threading.Tasks;
//using BTCPayServer.Ethereum.Client;
//using BTCPayServer.Ethereum.Events;
//using BTCPayServer.Ethereum.Services.Wallet;
//using BTCPayServer.Logging;
//using Microsoft.Extensions.Logging;
//using Nethereum.Hex.HexTypes;
//using Nethereum.RPC.Eth.DTOs;
//namespace BTCPayServer.Ethereum.Services
//{
//    public class NewBlockAndTransactionService
//    {
//        static NewBlockAndTransactionService()
//        {
//            lastBlockNumber = 0;
//        }

//        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
//        private static BigInteger lastBlockNumber;


//        static async Task GetLatestBlocksAsyncOld(EventAggregator _Aggregator, EthereumClient client, EthereumWallet ethereumWallet)
//        {
//            try
//            {
//                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

//                HexBigInteger blockNumber = await client.GetBlockNumber().ConfigureAwait(false);

//                if (lastBlockNumber == 0)//Init
//                {
//                    lastBlockNumber = blockNumber.Value;
//                }

//                if (lastBlockNumber > blockNumber.Value)//It seems , server is running and then we delete geth data then,it occured
//                {
//                    Logs.PayServer.LogInformation($"Eth, it seems ethereum data dir has been deleted, {lastBlockNumber} and CurrentBlockNumber {blockNumber.Value} ");

//                    lastBlockNumber = blockNumber.Value;
//                }

//                Logs.PayServer.LogInformation($"Eth LastBlockNumber {lastBlockNumber} and CurrentBlockNumber {blockNumber.Value} ");

//                if (lastBlockNumber < blockNumber.Value)
//                {
//                    while (lastBlockNumber <= blockNumber.Value)
//                    {
//                        Logs.PayServer.LogInformation($"Eth Iterate from LastBlockNumber {lastBlockNumber}");

//                        _Aggregator.Publish(new EthNewBlock(blockNumber.Value, ethereumWallet));
//                        BlockWithTransactions blockWithTransactions = await client.GetBlockWithTransactionsByNumber(lastBlockNumber);
//                        foreach (Transaction transaction in blockWithTransactions.Transactions)
//                        {
//                            Logs.PayServer.LogInformation($"Eth new TransactionHash {transaction.TransactionHash}");

//                            EthereumClientTransactionData ethereumClientTransactionData = new EthereumClientTransactionData();
//                            ethereumClientTransactionData.Init(transaction);
//                            _Aggregator.Publish(new EthNewTransactionEvent(ethereumWallet, ethereumClientTransactionData));
//                        }
//                        lastBlockNumber = lastBlockNumber + 1;
//                    }
//                }
//            }
//            catch
//            { }
//            finally
//            {
//                _semaphoreSlim.Release();
//            }

//        }


//        public static void GetLatestBlocksAsync(EventAggregator _Aggregator, EthereumClient client, EthereumWallet ethereumWallet)
//        {
//            client.PendingTransactionsSubscription.GetSubscribeResponseAsObservable().Subscribe(subscriptionId =>
//              Console.WriteLine("subscriptionId : " + subscriptionId)
//            );

//            client.PendingTransactionsSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(async transactionHash =>
//                 _Aggregator.Publish(new EthNewTransactionEvent(ethereumWallet, await client.GetTransactionAsyncByTransactionId(transactionHash).ConfigureAwait(false)))
//              );
//        }
//    }
//}
