using BTCPayServer.Security;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using AspNetCore;
#if NETCOREAPP21
using AspNet.Security.OpenIdConnect.Primitives;
#else
using OpenIdConnectConstants = OpenIddict.Abstractions.OpenIddictConstants;
#endif

namespace BTCPayServer.Controllers.RestApi
{
    public static class ControllersUtil
    {
        public const string ApiBersion1BaseUrl = "api/v1/[controller]";

        public static T GetControllerUtil<T>(this HttpContext context, IServiceProvider serviceProvider, string userId = null, string storeId = null, Claim[] additionalClaims = null) where T : Controller
        {
            return context.GetController<T>(serviceProvider, userId, additionalClaims, Security, new object[] { storeId }, SetStore);
        }

        static void SetStore(object[] objects, HttpContext context, IServiceProvider serviceProvider, string userId)
        {
            if (objects != null && objects.Any())
            {
                string storeId = objects[0].ToString();
                if (storeId != null)
                {
                    context.SetStoreData(serviceProvider.GetService<StoreRepository>().FindStore(storeId, userId).GetAwaiter().GetResult());
                }
            }
        }

        static void Security(HttpContext context, Claim[] additionalClaims, string userId)
        {
            if (userId != null)
            {
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(OpenIdConnectConstants.Claims.Subject, userId));
                if (additionalClaims != null)
                    claims.AddRange(additionalClaims);
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), Policies.CookieAuthentication));
            }
        }
    }
}
