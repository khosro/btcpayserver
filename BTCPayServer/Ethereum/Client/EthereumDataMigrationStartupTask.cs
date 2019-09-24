using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Ethereum.Client
{
    public class EthereumDataMigrationStartupTask : IStartupTask
    {
        private EthereumClientApplicationDbContextFactory _DBContextFactory;
        public EthereumDataMigrationStartupTask(EthereumClientApplicationDbContextFactory dbContextFactory)
        {
            _DBContextFactory = dbContextFactory;
        }
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Migrate(cancellationToken);

            }
            catch (Exception ex)
            {
                Logging.Logs.PayServer.LogError(ex, "Error on the EthereumDataMigrationStartupTask");
                throw;
            }
        }

        private async Task Migrate(CancellationToken cancellationToken)
        {
            using (CancellationTokenSource timeout = new CancellationTokenSource(10_000))
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken))
            {
retry:
                try
                {
                    await _DBContextFactory.CreateContext().Database.MigrateAsync();
                }
                // Starting up
                catch when (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch { }
                    goto retry;
                }
            }
        }
    }
}
