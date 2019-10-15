using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.FileManager;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Hosting
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            services.AddEthereumLike();
            services.AddFileServer();
        }
    }
}
