using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Hosting
{
    public static class CustomServices
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            services.AddEthereumLike();
            services.AddStartupTask<OpenIdStartupTask>();
            services.Configure<ApiBehaviorOptions>(options =>
            {
                /*
                 * https://stackoverflow.com/questions/55289631/inconsistent-behaviour-with-modelstate-validation-asp-net-core-api
                 * https://github.com/aspnet/AspNetCore/issues/6077
                 */
                options.SuppressModelStateInvalidFilter = true;
            });
        }
    }
}
