using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer
{
    public static class UtilitiesExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }
    }
    public static class BtcpayExtensions
    {
        public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
               where T : class, IStartupTask
               => services.AddTransient<IStartupTask, T>();
        public static async Task StartWithTasksAsync(this IWebHost webHost, CancellationToken cancellationToken = default)
        {
            // Load all tasks from DI
            var startupTasks = webHost.Services.GetServices<IStartupTask>();

            // Execute all the tasks
            foreach (var startupTask in startupTasks)
            {
                await startupTask.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            // Start the tasks as normal
            await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
