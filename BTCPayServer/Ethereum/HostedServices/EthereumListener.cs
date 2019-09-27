using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Logging;
using BTCPayServer.Services.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NBXplorer;
using System.Collections.Concurrent;
using NBXplorer.DerivationStrategy;
using BTCPayServer.Events;
using BTCPayServer.Services;
using BTCPayServer.Services.Wallets;
using NBitcoin;
using NBXplorer.Models;
using BTCPayServer.Payments;
using BTCPayServer.HostedServices;
using BTCPayServer.Ethereum.Events;
using BTCPayServer.Ethereum.Services.Wallet;
using EthereumXplorer.Client;
using EthereumXplorer;
using BTCPayServer.Ethereum.Payments;

namespace BTCPayServer.Ethereum.HostedServices
{
    public class EthereumListener
    {
    }

    public class NBXplorerListener : IHostedService
    {
        EventAggregator _Aggregator;
        EthereumExplorerClientProvider _ExplorerClients;
        Microsoft.Extensions.Hosting.IApplicationLifetime _Lifetime;
        InvoiceRepository _InvoiceRepository;
        private TaskCompletionSource<bool> _RunningTask;
        private CancellationTokenSource _Cts;
        EthereumWalletProvider _Wallets;

        public NBXplorerListener(EthereumExplorerClientProvider explorerClients,
                                EthereumWalletProvider wallets,
                                InvoiceRepository invoiceRepository,
                                EventAggregator aggregator,
                                Microsoft.Extensions.Hosting.IApplicationLifetime lifetime)
        {
            PollInterval = TimeSpan.FromMinutes(1.0);
            _Wallets = wallets;
            _InvoiceRepository = invoiceRepository;
            _ExplorerClients = explorerClients;
            _Aggregator = aggregator;
            _Lifetime = lifetime;
        }

        CompositeDisposable leases = new CompositeDisposable();
        ConcurrentDictionary<string, EthereumWebsocketNotificationSession> _SessionsByCryptoCode = new ConcurrentDictionary<string, EthereumWebsocketNotificationSession>();
        private Timer _ListenPoller;

        TimeSpan _PollInterval;
        public TimeSpan PollInterval
        {
            get
            {
                return _PollInterval;
            }
            set
            {
                _PollInterval = value;
                if (_ListenPoller != null)
                {
                    _ListenPoller.Change(0, (int)value.TotalMilliseconds);
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _RunningTask = new TaskCompletionSource<bool>();
            _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            leases.Add(_Aggregator.Subscribe<EthereumStateChangedEvent>(async evt =>
            {
                if (evt.NewState == EthereumState.Ready)
                {
                    var wallet = _Wallets.GetWallet(evt.Network);
                    if (_Wallets.IsAvailable(wallet.Network))
                    {
                        await Listen(wallet);
                    }
                }
            }));

            _ListenPoller = new Timer(async s =>
            {
                foreach (var wallet in _Wallets.GetWallets())
                {
                    if (_Wallets.IsAvailable(wallet.Network))
                    {
                        await Listen(wallet);
                    }
                }
            }, null, 0, (int)PollInterval.TotalMilliseconds);
            leases.Add(_ListenPoller);
            return Task.CompletedTask;
        }

        private async Task Listen(EthereumWallet wallet)
        {
            var network = wallet.Network;
            bool cleanup = false;
            try
            {
                if (_SessionsByCryptoCode.ContainsKey(network.CryptoCode))
                    return;
                var client = _ExplorerClients.GetExplorerClient(network);
                if (client == null)
                    return;
                if (_Cts.IsCancellationRequested)
                    return;
                var session = await client.CreateWebsocketNotificationSessionAsync(_Cts.Token).ConfigureAwait(false);
                if (!_SessionsByCryptoCode.TryAdd(network.CryptoCode, session))
                {
                    await session.DisposeAsync();
                    return;
                }
                cleanup = true;

                using (session)
                {
                 
                    Logs.PayServer.LogInformation($"{network.CryptoCode}: Checking if any pending invoice got paid while offline...");
                   //TODO. Impl it.
                    // int paymentCount = await FindPaymentViaPolling(wallet, network);
                    //Logs.PayServer.LogInformation($"{network.CryptoCode}: {paymentCount} payments happened while offline");

                    Logs.PayServer.LogInformation($"Connected to WebSocket of NBXplorer ({network.CryptoCode})");
                    while (!_Cts.IsCancellationRequested)
                    {
                        var newEvent = await session.NextEventAsync(_Cts.Token).ConfigureAwait(false);
                        switch (newEvent)
                        {
                            case EthNewBlockEvent evt:

                                //await Task.WhenAll((await _InvoiceRepository.GetPendingInvoices())
                                //    .Select(invoiceId => UpdatePaymentStates(wallet, invoiceId))
                                //    .ToArray());
                                //_Aggregator.Publish(new EthNewBlockEvent() { CryptoCode = evt.CryptoCode });
                                break;
                            case EthNewTransactionEvent evt:
                                Logs.PayServer.LogInformation($"New EthNewTransactionEvent {evt.Transaction.TransactionHash}");
                                //wallet.InvalidateCache(evt.DerivationStrategy);
                                //foreach (var output in evt.Outputs)
                                //{
                                //    foreach (var txCoin in evt.TransactionData.Transaction.Outputs.AsCoins()
                                //                                                .Where(o => o.ScriptPubKey == output.ScriptPubKey))
                                //    {
                                //        var invoice = await _InvoiceRepository.GetInvoiceFromScriptPubKey(output.ScriptPubKey, network.CryptoCode);
                                //        if (invoice != null)
                                //        {
                                //            var paymentData = new BitcoinLikePaymentData(txCoin, evt.TransactionData.Transaction.RBF);
                                //            var alreadyExist = GetAllBitcoinPaymentData(invoice).Where(c => c.GetPaymentId() == paymentData.GetPaymentId()).Any();
                                //            if (!alreadyExist)
                                //            {
                                //                var payment = await _InvoiceRepository.AddPayment(invoice.Id, DateTimeOffset.UtcNow, paymentData, network);
                                //                if (payment != null)
                                //                    await ReceivedPayment(wallet, invoice, payment, evt.DerivationStrategy);
                                //            }
                                //            else
                                //            {
                                //                await UpdatePaymentStates(wallet, invoice.Id);
                                //            }
                                //        }
                                //    }
                                //}
                                break;
                            default:
                                Logs.PayServer.LogWarning("Received unknown message from NBXplorer");
                                break;
                        }
                    }
                }
            }
            catch when (_Cts.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Logs.PayServer.LogError(ex, $"Error while connecting to WebSocket of NBXplorer ({network.CryptoCode})");
            }
            finally
            {
                if (cleanup)
                {
                    Logs.PayServer.LogInformation($"Disconnected from WebSocket of NBXplorer ({network.CryptoCode})");
                    _SessionsByCryptoCode.TryRemove(network.CryptoCode, out EthereumWebsocketNotificationSession unused);
                    if (_SessionsByCryptoCode.Count == 0 && _Cts.IsCancellationRequested)
                    {
                        _RunningTask.TrySetResult(true);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            leases.Dispose();
            _Cts.Cancel();
            return Task.WhenAny(_RunningTask.Task, Task.Delay(-1, cancellationToken));
        }


        IEnumerable<EthereumLikePaymentData> GetAllBitcoinPaymentData(InvoiceEntity invoice)
        {
            return invoice.GetPayments()
                    .Where(p => p.GetPaymentMethodId().PaymentType == PaymentTypes.BTCLike)
                    .Select(p => (EthereumLikePaymentData)p.GetCryptoPaymentData());
        }

        //async Task<InvoiceEntity> UpdatePaymentStates(EthereumWallet wallet, string invoiceId)
        //{
        //    var invoice = await _InvoiceRepository.GetInvoice(invoiceId, false);
        //    if (invoice == null)
        //        return null;
        //    List<PaymentEntity> updatedPaymentEntities = new List<PaymentEntity>();
        //    var transactions = await wallet.GetTransactions(GetAllBitcoinPaymentData(invoice)
        //            //.Select(p => p.Outpoint.Hash)
        //            .ToArray());
        //    var conflicts = GetConflicts(transactions.Select(t => t.Value));
        //    foreach (var payment in invoice.GetPayments(wallet.Network))
        //    {
        //        if (payment.GetPaymentMethodId().PaymentType != PaymentTypes.BTCLike)
        //            continue;
        //        var paymentData = (EthereumLikePaymentData)payment.GetCryptoPaymentData();
        //      //  if (!transactions.TryGetValue(paymentData.Outpoint.Hash, out TransactionResult tx))
        //      //      continue;
        //        var txId = tx.Transaction.GetHash();
        //        var txConflict = conflicts.GetConflict(txId);
        //        var accounted = txConflict == null || txConflict.IsWinner(txId);

        //        bool updated = false;
        //        if (accounted != payment.Accounted)
        //        {
        //            updated = true;
        //            payment.Accounted = accounted;
        //        }

        //        if (paymentData.ConfirmationCount != tx.Confirmations)
        //        {
        //            if (wallet.Network.MaxTrackedConfirmation >= paymentData.ConfirmationCount)
        //            {
        //                paymentData.ConfirmationCount = tx.Confirmations;
        //                payment.SetCryptoPaymentData(paymentData);
        //                updated = true;
        //            }
        //        }

        //        // if needed add invoice back to pending to track number of confirmations
        //        if (paymentData.ConfirmationCount < wallet.Network.MaxTrackedConfirmation)
        //            await _InvoiceRepository.AddPendingInvoiceIfNotPresent(invoice.Id);

        //        if (updated)
        //            updatedPaymentEntities.Add(payment);
        //    }
        //    await _InvoiceRepository.UpdatePayments(updatedPaymentEntities);
        //    if (updatedPaymentEntities.Count != 0)
        //        _Aggregator.Publish(new InvoiceNeedUpdateEvent(invoice.Id));
        //    return invoice;
        //}

        class TransactionConflict
        {
            public Dictionary<uint256, TransactionResult> Transactions { get; set; } = new Dictionary<uint256, TransactionResult>();


            uint256 _Winner;
            public bool IsWinner(uint256 txId)
            {
                if (_Winner == null)
                {
                    var confirmed = Transactions.FirstOrDefault(t => t.Value.Confirmations >= 1);
                    if (!confirmed.Equals(default(KeyValuePair<uint256, TransactionResult>)))
                    {
                        _Winner = confirmed.Key;
                    }
                    else
                    {
                        // Take the most recent (bitcoin node would not forward a conflict without a successful RBF)
                        _Winner = Transactions
                                .OrderByDescending(t => t.Value.Timestamp)
                                .First()
                                .Key;
                    }
                }
                return _Winner == txId;
            }
        }
        class TransactionConflicts : List<TransactionConflict>
        {
            public TransactionConflicts(IEnumerable<TransactionConflict> collection) : base(collection)
            {

            }

            public TransactionConflict GetConflict(uint256 txId)
            {
                return this.FirstOrDefault(c => c.Transactions.ContainsKey(txId));
            }
        }
        private TransactionConflicts GetConflicts(IEnumerable<TransactionResult> transactions)
        {
            Dictionary<OutPoint, TransactionConflict> conflictsByOutpoint = new Dictionary<OutPoint, TransactionConflict>();
            foreach (var tx in transactions)
            {
                var hash = tx.Transaction.GetHash();
                foreach (var input in tx.Transaction.Inputs)
                {
                    TransactionConflict conflict = new TransactionConflict();
                    if (!conflictsByOutpoint.TryAdd(input.PrevOut, conflict))
                    {
                        conflict = conflictsByOutpoint[input.PrevOut];
                    }
                    if (!conflict.Transactions.ContainsKey(hash))
                        conflict.Transactions.Add(hash, tx);
                }
            }
            return new TransactionConflicts(conflictsByOutpoint.Where(c => c.Value.Transactions.Count > 1).Select(c => c.Value));
        }

    
        //private async Task<int> FindPaymentViaPolling(BTCPayWallet wallet, BTCPayNetworkBase network)
        //{
        //    int totalPayment = 0;
        //    var invoices = await _InvoiceRepository.GetPendingInvoices();
        //    foreach (var invoiceId in invoices)
        //    {
        //        var invoice = await _InvoiceRepository.GetInvoice(invoiceId, true);
        //        if (invoice == null)
        //            continue;
        //        var alreadyAccounted = GetAllBitcoinPaymentData(invoice).Select(p => p.Outpoint).ToHashSet();
        //        var strategy = GetDerivationStrategy(invoice, network);
        //        if (strategy == null)
        //            continue;
        //        var cryptoId = new PaymentMethodId(network.CryptoCode, PaymentTypes.BTCLike);
        //        if (!invoice.Support(cryptoId))
        //            continue;
        //        var coins = (await wallet.GetUnspentCoins(strategy))
        //                     .Where(c => invoice.AvailableAddressHashes.Contains(c.Coin.ScriptPubKey.Hash.ToString() + cryptoId))
        //                     .ToArray();
        //        foreach (var coin in coins.Where(c => !alreadyAccounted.Contains(c.Coin.Outpoint)))
        //        {
        //            var transaction = await wallet.GetTransactionAsync(coin.Coin.Outpoint.Hash);
        //            var paymentData = new EthereumLikePaymentData();
        //           // var paymentData = new EthereumLikePaymentData(coin.Coin, transaction.Transaction.RBF);
        //            var payment = await _InvoiceRepository.AddPayment(invoice.Id, coin.Timestamp, paymentData, network).ConfigureAwait(false);
        //            alreadyAccounted.Add(coin.Coin.Outpoint);
        //            if (payment != null)
        //            {
        //                invoice = await ReceivedPayment(wallet, invoice, payment, strategy);
        //                if (invoice == null)
        //                    continue;
        //                totalPayment++;
        //            }
        //        }
        //    }
        //    return totalPayment;
        //}

        //private DerivationStrategyBase GetDerivationStrategy(InvoiceEntity invoice, BTCPayNetworkBase network)
        //{
        //    return invoice.GetSupportedPaymentMethod<DerivationSchemeSettings>(new PaymentMethodId(network.CryptoCode, PaymentTypes.BTCLike))
        //                  .Select(d => d.AccountDerivation)
        //                  .FirstOrDefault();
        //}

        //private async Task<InvoiceEntity> ReceivedPayment(BTCPayWallet wallet, InvoiceEntity invoice, PaymentEntity payment, DerivationStrategyBase strategy)
        //{
        //    var paymentData = (EthereumLikePaymentData)payment.GetCryptoPaymentData();
        //    invoice = (await UpdatePaymentStates(wallet, invoice.Id));
        //    if (invoice == null)
        //        return null;
        //    var paymentMethod = invoice.GetPaymentMethod(wallet.Network, PaymentTypes.BTCLike);
        //    if (paymentMethod != null &&
        //        paymentMethod.GetPaymentMethodDetails() is EthereumPaymentMethodDetails btc &&
        //        btc.GetDepositAddress(wallet.Network.NBitcoinNetwork).ScriptPubKey == paymentData.Output.ScriptPubKey &&
        //        paymentMethod.Calculate().Due > Money.Zero)
        //    {
        //        var address = await wallet.ReserveAddressAsync(strategy);
        //        btc.DepositAddress = address.ToString();
        //        await _InvoiceRepository.NewAddress(invoice.Id, btc, wallet.Network);
        //        _Aggregator.Publish(new InvoiceNewAddressEvent(invoice.Id, address.ToString(), wallet.Network));
        //        paymentMethod.SetPaymentMethodDetails(btc);
        //        invoice.SetPaymentMethod(paymentMethod);
        //    }
        //    wallet.InvalidateCache(strategy);
        //    _Aggregator.Publish(new InvoiceEvent(invoice, 1002, InvoiceEvent.ReceivedPayment) { Payment = payment });
        //    return invoice;
        //}
        //public Task StopAsync(CancellationToken cancellationToken)
        //{
        //    leases.Dispose();
        //    _Cts.Cancel();
        //    return Task.WhenAny(_RunningTask.Task, Task.Delay(-1, cancellationToken));
        //}
    }
}
