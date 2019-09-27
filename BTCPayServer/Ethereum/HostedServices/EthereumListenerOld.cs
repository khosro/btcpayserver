//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using BTCPayServer.Ethereum.Client;
//using BTCPayServer.Ethereum.Events;
//using BTCPayServer.Ethereum.Services;
//using BTCPayServer.Ethereum.Services.Wallet;
//using BTCPayServer.HostedServices;
//using BTCPayServer.Logging;
//using BTCPayServer.Payments;
//using BTCPayServer.Payments.Bitcoin;
//using BTCPayServer.Services.Invoices;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using NBitcoin;
//using NBXplorer;
//using NBXplorer.DerivationStrategy;
//using NBXplorer.Models;

//namespace BTCPayServer.Ethereum.HostedServices
//{
//    public class EthereumListener : IHostedService
//    {
//        private EventAggregator _Aggregator;
//        private EthereumClientProvider _EthereumClients;
//        private readonly Microsoft.Extensions.Hosting.IApplicationLifetime _Lifetime;
//        private InvoiceRepository _InvoiceRepository;
//        private TaskCompletionSource<bool> _RunningTask;
//        private CancellationTokenSource _Cts;
//        private EthereumWalletProvider _Wallets;
//        private EthereumClientTransactionRepository _ethereumClientTransactionRepository;
//        public EthereumListener(EthereumClientProvider EthereumClients,
//                                EthereumWalletProvider wallets,
//                                InvoiceRepository invoiceRepository,
//                                EventAggregator aggregator,
//                                IApplicationLifetime lifetime,
//                                 EthereumClientTransactionRepository ethereumClientTransactionRepository)
//        {
//            PollInterval = TimeSpan.FromMinutes(1.0);
//            _Wallets = wallets;
//            _InvoiceRepository = invoiceRepository;
//            _EthereumClients = EthereumClients;
//            _Aggregator = aggregator;
//            _Lifetime = lifetime;
//            _ethereumClientTransactionRepository = ethereumClientTransactionRepository;
//        }

//        private CompositeDisposable leases = new CompositeDisposable();
//        private ConcurrentDictionary<string, string> _SessionsByCryptoCode = new ConcurrentDictionary<string, string>();
//        private Timer _ListenPoller;
//        private TimeSpan _PollInterval;
//        public TimeSpan PollInterval
//        {
//            get
//            {
//                return _PollInterval;
//            }
//            set
//            {
//                _PollInterval = value;
//                if (_ListenPoller != null)
//                {
//                    _ListenPoller.Change(0, (int)value.TotalMilliseconds);
//                }
//            }
//        }

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            _RunningTask = new TaskCompletionSource<bool>();
//            _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//            leases.Add(_Aggregator.Subscribe<EthereumStateChangedEvent>(async evt =>
//            {
//                if (evt.NewState == EthereumState.Ready)
//                {
//                    EthereumWallet wallet = _Wallets.GetWallet(evt.Network);
//                    if (_Wallets.IsAvailable(wallet.Network))
//                    {
//                        await Listen(wallet);
//                    }
//                }
//            }));

//            leases.Add(_Aggregator.Subscribe<EthNewTransactionEvent>(async evt =>
//            {
//                Logs.PayServer.LogInformation($"Publish subscribe EthNewTransactionEvent ,TransactionHash : {evt.Transaction.TransactionHash}");
//                await _ethereumClientTransactionRepository.SaveOrUpdateTransaction(evt.Transaction);
//                await NewTransactionEvent(evt.EthereumWallet);
//            }));

//            leases.Add(_Aggregator.Subscribe<EthNewBlock>(async evt =>
//            {
//                Logs.PayServer.LogInformation($"Publish subscribe EthNewBlock, BlockNumber : {evt.BlockNumber}");
//                await Task.WhenAll((await _InvoiceRepository.GetPendingInvoices())
//                                     .Select(invoiceId => UpdatePaymentStates(evt.EthereumWallet, invoiceId))
//                                     .ToArray());
//                _Aggregator.Publish(new BTCPayServer.Events.NewBlockEvent() { CryptoCode = evt.EthereumWallet.Network.CryptoCode });
//            }));

//            _ListenPoller = new Timer(async s =>
//            {
//                foreach (EthereumWallet wallet in _Wallets.GetWallets())
//                {
//                    if (_Wallets.IsAvailable(wallet.Network))
//                    {
//                        await Listen(wallet);
//                    }
//                }
//            }, null, 0, (int)PollInterval.TotalMilliseconds);
//            leases.Add(_ListenPoller);

//            return Task.CompletedTask;
//        }

//        private async Task Listen(EthereumWallet wallet)
//        {
//            //return;
//            EthereumLikecBtcPayNetwork network = wallet.Network;
//            bool cleanup = false;
//            try
//            {
//                if (_SessionsByCryptoCode.ContainsKey(network.CryptoCode))
//                {
//                    return;
//                }

//                EthereumClient client = _EthereumClients.GetEthereumClient(network);
//                if (client == null)
//                {
//                    return;
//                }

//                if (_Cts.IsCancellationRequested)
//                {
//                    return;
//                }
//                if (!_SessionsByCryptoCode.TryAdd(network.CryptoCode, "Added"))
//                {
//                    return;
//                }

//                Logs.PayServer.LogInformation($"{network.CryptoCode}: Checking if any pending invoice got paid while offline...");
//                int paymentCount = await FindPaymentViaPolling(wallet, network);
//                Logs.PayServer.LogInformation($"{network.CryptoCode}: {paymentCount} payments happened while offline");

//                /*
//                 * high CPU usage.
//                 * cleanup = true;
//                while (!_Cts.IsCancellationRequested)
//                {
//                    //Logs.PayServer.LogInformation($"Calling NewBlockAndTransactionService.GetLatestBlocksAsync");
//                     await NewBlockAndTransactionService.GetLatestBlocksAsync(_Aggregator, client, wallet);
//                }
//                */

//                /*
//                 //If we use old code GetLatestBlocksAsyncOld then we must use timer.
//                 * bool isRunning = false;
//                var newBlockAndTransactionService = new Timer(async s =>
//                {
//                    if (!isRunning)
//                    {
//                        isRunning = true;
//                        Logs.PayServer.LogInformation($"Eth, isRunning {isRunning}");
//                        NewBlockAndTransactionService.GetLatestBlocksAsync(_Aggregator, client, wallet);
//                        isRunning = false;
//                        Logs.PayServer.LogInformation($"Eth, isRunning {isRunning}");
//                    }
//                    else
//                    {
//                        Logs.PayServer.LogInformation($"Eth is already running, isRunning {isRunning}");
//                    }
//                }, null, 0, (int)TimeSpan.FromSeconds(30.0).TotalMilliseconds);
//                leases.Add(newBlockAndTransactionService);
//                */
//                NewBlockAndTransactionService.GetLatestBlocksAsync(_Aggregator, client, wallet);

//            }
//            catch when (_Cts.IsCancellationRequested)
//            {
//                cleanup = true;//If we use timer
//            }
//            catch (Exception ex)
//            {
//                cleanup = true;//If we use timer
//                Logs.PayServer.LogError(ex, $"Error while connecting to NewBlockAndTransactionService of Eth ({network.CryptoCode})");
//            }
//            finally
//            {
//                if (cleanup)//TODO.It does not call on app shutdown
//                {
//                    Logs.PayServer.LogInformation($"Disconnected from NewBlockAndTransactionService of Eth ({network.CryptoCode})");
//                    _SessionsByCryptoCode.TryRemove(network.CryptoCode, out string unused);
//                    if (_SessionsByCryptoCode.Count == 0 && _Cts.IsCancellationRequested)
//                    {
//                        _RunningTask.TrySetResult(true);
//                    }
//                }
//            }
//        }

//        private async Task NewTransactionEvent(EthereumWallet wallet/*, NewTransactionEvent evt*/)
//        {
//            //TODO.Must refactor based on Eth concepts
//            EthereumLikecBtcPayNetwork network = wallet.Network;
//            /*  wallet.InvalidateCache(evt.DerivationStrategy);
//              foreach (MatchedOutput output in evt.Outputs)
//              {
//                  foreach (Coin txCoin in evt.TransactionData.Transaction.Outputs.AsCoins()
//                                                              .Where(o => o.ScriptPubKey == output.ScriptPubKey))
//                  {
//                      InvoiceEntity invoice = await _InvoiceRepository.GetInvoiceFromScriptPubKey(output.ScriptPubKey, network.CryptoCode);
//                      if (invoice != null)
//                      {
//                          var paymentData = new BitcoinLikePaymentData(txCoin, evt.TransactionData.Transaction.RBF);
//                          var alreadyExist = GetAllBitcoinPaymentData(invoice).Where(c => c.GetPaymentId() == paymentData.GetPaymentId()).Any();
//                          if (!alreadyExist)
//                          {
//                              PaymentEntity payment = await _InvoiceRepository.AddPayment(invoice.Id, DateTimeOffset.UtcNow, paymentData, network);
//                              if (payment != null)
//                              {
//                                  await ReceivedPayment(wallet, invoice, payment, evt.DerivationStrategy);
//                              }
//                          }
//                          else
//                          {
//                              await UpdatePaymentStates(wallet, invoice.Id);
//                          }
//                      }
//                  }
//              }*/
//        }


//        private IEnumerable<BitcoinLikePaymentData> GetAllBitcoinPaymentData(InvoiceEntity invoice)
//        {
//            return invoice.GetPayments()
//                    .Where(p => p.GetPaymentMethodId().PaymentType == PaymentTypes.BTCLike)
//                    .Select(p => (BitcoinLikePaymentData)p.GetCryptoPaymentData());
//        }

//        private async Task<InvoiceEntity> UpdatePaymentStates(EthereumWallet wallet, string invoiceId)
//        {
//            //TODO.Change impl
//            InvoiceEntity invoice = await _InvoiceRepository.GetInvoice(invoiceId, false);
//            /* if (invoice == null)
//             {
//                 return null;
//             }

//             List<PaymentEntity> updatedPaymentEntities = new List<PaymentEntity>();
//             Dictionary<uint256, TransactionResult> transactions = await wallet.GetTransactions(GetAllBitcoinPaymentData(invoice)
//                     .Select(p => p.Outpoint.Hash)
//                     .ToArray());
//             TransactionConflicts conflicts = GetConflicts(transactions.Select(t => t.Value));
//             foreach (PaymentEntity payment in invoice.GetPayments(wallet.Network))
//             {
//                 if (payment.GetPaymentMethodId().PaymentType != PaymentTypes.BTCLike)
//                 {
//                     continue;
//                 }

//                 var paymentData = (BitcoinLikePaymentData)payment.GetCryptoPaymentData();
//                 if (!transactions.TryGetValue(paymentData.Outpoint.Hash, out TransactionResult tx))
//                 {
//                     continue;
//                 }

//                 uint256 txId = tx.Transaction.GetHash();
//                 TransactionConflict txConflict = conflicts.GetConflict(txId);
//                 var accounted = txConflict == null || txConflict.IsWinner(txId);

//                 bool updated = false;
//                 if (accounted != payment.Accounted)
//                 {
//                     updated = true;
//                     payment.Accounted = accounted;
//                 }

//                 if (paymentData.ConfirmationCount != tx.Confirmations)
//                 {
//                     if (wallet.Network.MaxTrackedConfirmation >= paymentData.ConfirmationCount)
//                     {
//                         paymentData.ConfirmationCount = tx.Confirmations;
//                         payment.SetCryptoPaymentData(paymentData);
//                         updated = true;
//                     }
//                 }

//                 // if needed add invoice back to pending to track number of confirmations
//                 if (paymentData.ConfirmationCount < wallet.Network.MaxTrackedConfirmation)
//                 {
//                     await _InvoiceRepository.AddPendingInvoiceIfNotPresent(invoice.Id);
//                 }

//                 if (updated)
//                 {
//                     updatedPaymentEntities.Add(payment);
//                 }
//             }
//             await _InvoiceRepository.UpdatePayments(updatedPaymentEntities);
//             if (updatedPaymentEntities.Count != 0)
//             {
//                 _Aggregator.Publish(new InvoiceNeedUpdateEvent(invoice.Id));
//             }
//             */
//            return invoice;
//        }

//        private class TransactionConflict
//        {
//            public Dictionary<uint256, TransactionResult> Transactions { get; set; } = new Dictionary<uint256, TransactionResult>();

//            private uint256 _Winner;
//            public bool IsWinner(uint256 txId)
//            {
//                if (_Winner == null)
//                {
//                    KeyValuePair<uint256, TransactionResult> confirmed = Transactions.FirstOrDefault(t => t.Value.Confirmations >= 1);
//                    if (!confirmed.Equals(default(KeyValuePair<uint256, TransactionResult>)))
//                    {
//                        _Winner = confirmed.Key;
//                    }
//                    else
//                    {
//                        // Take the most recent (bitcoin node would not forward a conflict without a successful RBF)
//                        _Winner = Transactions
//                                .OrderByDescending(t => t.Value.Timestamp)
//                                .First()
//                                .Key;
//                    }
//                }
//                return _Winner == txId;
//            }
//        }

//        private class TransactionConflicts : List<TransactionConflict>
//        {
//            public TransactionConflicts(IEnumerable<TransactionConflict> collection) : base(collection)
//            {

//            }

//            public TransactionConflict GetConflict(uint256 txId)
//            {
//                return this.FirstOrDefault(c => c.Transactions.ContainsKey(txId));
//            }
//        }
//        private TransactionConflicts GetConflicts(IEnumerable<TransactionResult> transactions)
//        {
//            Dictionary<OutPoint, TransactionConflict> conflictsByOutpoint = new Dictionary<OutPoint, TransactionConflict>();
//            foreach (TransactionResult tx in transactions)
//            {
//                uint256 hash = tx.Transaction.GetHash();
//                foreach (TxIn input in tx.Transaction.Inputs)
//                {
//                    TransactionConflict conflict = new TransactionConflict();
//                    if (!conflictsByOutpoint.TryAdd(input.PrevOut, conflict))
//                    {
//                        conflict = conflictsByOutpoint[input.PrevOut];
//                    }
//                    if (!conflict.Transactions.ContainsKey(hash))
//                    {
//                        conflict.Transactions.Add(hash, tx);
//                    }
//                }
//            }
//            return new TransactionConflicts(conflictsByOutpoint.Where(c => c.Value.Transactions.Count > 1).Select(c => c.Value));
//        }
//        //TODO.Change impl
//        private async Task<int> FindPaymentViaPolling(EthereumWallet wallet, BTCPayNetworkBase network)
//        {
//            int totalPayment = 0;
//            /* var invoices = await _InvoiceRepository.GetPendingInvoices();
//             foreach (var invoiceId in invoices)
//             {
//                 InvoiceEntity invoice = await _InvoiceRepository.GetInvoice(invoiceId, true);
//                 if (invoice == null)
//                 {
//                     continue;
//                 }

//                 var alreadyAccounted = GetAllBitcoinPaymentData(invoice).Select(p => p.Outpoint).ToHashSet();
//                 DerivationStrategyBase strategy = GetDerivationStrategy(invoice, network);
//                 if (strategy == null)
//                 {
//                     continue;
//                 }

//                 var cryptoId = new PaymentMethodId(network.CryptoCode, PaymentTypes.BTCLike);
//                 if (!invoice.Support(cryptoId))
//                 {
//                     continue;
//                 }

//                 ReceivedCoin[] coins = (await wallet.GetUnspentCoins(strategy))
//                              .Where(c => invoice.AvailableAddressHashes.Contains(c.Coin.ScriptPubKey.Hash.ToString() + cryptoId))
//                              .ToArray();
//                 foreach (var coin in coins.Where(c => !alreadyAccounted.Contains(c.Coin.Outpoint)))
//                 {
//                     var transaction = await wallet.GetTransactionAsync(coin.Coin.Outpoint.Hash);
//                     var paymentData = new BitcoinLikePaymentData(coin.Coin, transaction.Transaction.RBF);
//                     var payment = await _InvoiceRepository.AddPayment(invoice.Id, coin.Timestamp, paymentData, network).ConfigureAwait(false);
//                     alreadyAccounted.Add(coin.Coin.Outpoint);
//                     if (payment != null)
//                     {
//                         invoice = await ReceivedPayment(wallet, invoice, payment, strategy);
//                         if (invoice == null)
//                         {
//                             continue;
//                         }

//                         totalPayment++;
//                     }
//                 }
//             }*/
//            return totalPayment;
//        }

//        private DerivationStrategyBase GetDerivationStrategy(InvoiceEntity invoice, BTCPayNetworkBase network)
//        {
//            return invoice.GetSupportedPaymentMethod<DerivationSchemeSettings>(new PaymentMethodId(network.CryptoCode, PaymentTypes.BTCLike))
//                          .Select(d => d.AccountDerivation)
//                          .FirstOrDefault();
//        }

//        //TODO.Change impl
//        private async Task<InvoiceEntity> ReceivedPayment(EthereumWallet wallet, InvoiceEntity invoice, PaymentEntity payment, DerivationStrategyBase strategy)
//        {
//            var paymentData = (BitcoinLikePaymentData)payment.GetCryptoPaymentData();
//            invoice = (await UpdatePaymentStates(wallet, invoice.Id));
//            /* if (invoice == null)
//             {
//                 return null;
//             }

//             PaymentMethod paymentMethod = invoice.GetPaymentMethod(wallet.Network, PaymentTypes.BTCLike);
//             if (paymentMethod != null &&
//                 paymentMethod.GetPaymentMethodDetails() is BitcoinLikeOnChainPaymentMethod btc &&
//                 btc.GetDepositAddress(wallet.Network.NBitcoinNetwork).ScriptPubKey == paymentData.Output.ScriptPubKey &&
//                 paymentMethod.Calculate().Due > Money.Zero)
//             {
//                 BitcoinAddress address = await wallet.ReserveAddressAsync(strategy);
//                 btc.DepositAddress = address.ToString();
//                 await _InvoiceRepository.NewAddress(invoice.Id, btc, wallet.Network);
//                 _Aggregator.Publish(new InvoiceNewAddressEvent(invoice.Id, address.ToString(), wallet.Network));
//                 paymentMethod.SetPaymentMethodDetails(btc);
//                 invoice.SetPaymentMethod(paymentMethod);
//             }
//             wallet.InvalidateCache(strategy);
//             _Aggregator.Publish(new InvoiceEvent(invoice, 1002, InvoiceEvent.ReceivedPayment) { Payment = payment });
//             */
//            return invoice;
//        }
//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            leases.Dispose();
//            _Cts.Cancel();
//            return Task.WhenAny(_RunningTask.Task, Task.Delay(-1, cancellationToken));
//        }
//    }
//}
