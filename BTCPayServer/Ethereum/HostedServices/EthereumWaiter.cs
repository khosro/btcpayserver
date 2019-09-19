using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Ethereum.Client;
using BTCPayServer.Ethereum.Events;
using BTCPayServer.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace BTCPayServer.HostedServices
{
    public enum EthereumState
    {
        NotConnected,
        Synching,
        Ready
    }
    public class EthereumStatusResult
    {
        public bool IsFullySynched { get; set; }
        public BigInteger CurrentHeight { get; set; }
        public BigInteger ChainHeight { get; set; }
        public string CryptoCode { get; set; }
        public string Version { get; set; }
    }

    public class EthereumDashboard
    {
        public class EthereumSummary
        {
            public EthereumLikecBtcPayNetwork Network { get; set; }
            public EthereumState State { get; set; }
            public EthereumStatusResult Status { get; set; }
            public string Error { get; set; }
        }

        private ConcurrentDictionary<string, EthereumSummary> _Summaries = new ConcurrentDictionary<string, EthereumSummary>();

        public void Publish(EthereumLikecBtcPayNetwork network, EthereumState state, EthereumStatusResult status, string error)
        {
            var summary = new EthereumSummary() { Network = network, State = state, Status = status, Error = error };
            _Summaries.AddOrUpdate(network.CryptoCode, summary, (k, v) => summary);
        }

        public bool IsFullySynched()
        {
            return _Summaries.All(s => s.Value.Status != null && s.Value.Status.IsFullySynched);
        }

        public bool IsFullySynched(string cryptoCode, out EthereumSummary summary)
        {
            return _Summaries.TryGetValue(cryptoCode.ToUpperInvariant(), out summary) &&
                   summary.Status != null && summary.Status.IsFullySynched;
        }
        public EthereumSummary Get(string cryptoCode)
        {
            _Summaries.TryGetValue(cryptoCode.ToUpperInvariant(), out EthereumSummary summary);
            return summary;
        }
        public IEnumerable<EthereumSummary> GetAll()
        {
            return _Summaries.Values;
        }
    }

    public class EthereumWaiters : IHostedService
    {
        private List<EthereumWaiter> _Waiters = new List<EthereumWaiter>();
        public EthereumWaiters(EthereumDashboard dashboard, EthereumClientProvider clientProvider, EventAggregator eventAggregator)
        {
            foreach ((EthereumLikecBtcPayNetwork, EthereumClient) client in clientProvider.GetAll())
            {
                _Waiters.Add(new EthereumWaiter(dashboard, client.Item1, client.Item2, eventAggregator));
            }
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_Waiters.Select(w => w.StartAsync(cancellationToken)).ToArray());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_Waiters.Select(w => w.StopAsync(cancellationToken)).ToArray());
        }
    }

    public class EthereumWaiter : IHostedService
    {

        public EthereumWaiter(EthereumDashboard dashboard, EthereumLikecBtcPayNetwork network, EthereumClient client, EventAggregator aggregator)
        {
            _Network = network;
            _Client = client;
            _Aggregator = aggregator;
            _Dashboard = dashboard;
        }

        private EthereumDashboard _Dashboard;
        private EthereumLikecBtcPayNetwork _Network;
        private EventAggregator _Aggregator;
        private EthereumClient _Client;
        private CancellationTokenSource _Cts;
        private Task _Loop;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _Loop = StartLoop(_Cts.Token);
            return Task.CompletedTask;
        }

        private async Task StartLoop(CancellationToken cancellation)
        {
            Logs.PayServer.LogInformation($"Starting listening Ethereum ({_Network.CryptoCode})");
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        while (await StepAsync(cancellation))
                        {

                        }
                        await Task.Delay(PollInterval, cancellation);
                    }
                    catch (Exception ex) when (!cancellation.IsCancellationRequested)
                    {
                        Logs.PayServer.LogError(ex, $"Unhandled exception in EthereumWaiter ({_Network.CryptoCode})");
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellation);
                    }
                }
            }
            catch when (cancellation.IsCancellationRequested) { }
        }

        private async Task<bool> StepAsync(CancellationToken cancellation)
        {
            EthereumState oldState = State;
            string error = null;
            EthereumStatusResult status = null;
            try
            {
                switch (State)
                {
                    case EthereumState.NotConnected:
                        status = await _Client.GetStatusAsync(cancellation);
                        if (status != null)
                        {
                            if (status.IsFullySynched)
                            {
                                State = EthereumState.Ready;
                            }
                            else
                            {
                                State = EthereumState.Synching;
                            }
                        }
                        break;
                    case EthereumState.Synching:
                        status = await _Client.GetStatusAsync(cancellation);
                        if (status == null)
                        {
                            State = EthereumState.NotConnected;
                        }
                        else if (status.IsFullySynched)
                        {
                            State = EthereumState.Ready;
                        }
                        break;
                    case EthereumState.Ready:
                        status = await _Client.GetStatusAsync(cancellation);
                        if (status == null)
                        {
                            State = EthereumState.NotConnected;
                        }
                        else if (!status.IsFullySynched)
                        {
                            State = EthereumState.Synching;
                        }
                        break;
                }

            }
            catch (Exception ex) when (!cancellation.IsCancellationRequested)
            {
                error = ex.Message;
            }


            if (status == null && error == null)
            {
                error = $"{_Network.CryptoCode}: Ethereum does not support this cryptocurrency";
            }

            if (error != null)
            {
                State = EthereumState.NotConnected;
                status = null;
                Logs.PayServer.LogError($"{_Network.CryptoCode}: Ethereum error `{error}`");
            }

            _Dashboard.Publish(_Network, State, status, error);
            if (oldState != State)
            {
                if (State == EthereumState.Synching)
                {
                    PollInterval = TimeSpan.FromSeconds(10);
                }
                else
                {
                    PollInterval = TimeSpan.FromMinutes(1);
                }
                _Aggregator.Publish(new EthereumStateChangedEvent(_Network, oldState, State));
            }
            return oldState != State;
        }

        public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(1.0);

        public EthereumState State { get; private set; }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Cts.Cancel();
            return _Loop;
        }
    }
}
