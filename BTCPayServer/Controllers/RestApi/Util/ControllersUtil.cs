using BTCPayServer.Security;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;
#if NETCOREAPP21
using AspNet.Security.OpenIdConnect.Primitives;
#else
using OpenIdConnectConstants = OpenIddict.Abstractions.OpenIddictConstants;
#endif

namespace BTCPayServer.Controllers.RestApi
{
    public static class ControllersUtil
    {
        public static T GetController<T>(HttpContext context, IServiceProvider serviceProvider, string userId = null, string storeId = null, Claim[] additionalClaims = null) where T : Controller
        {
            if (userId != null)
            {
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(OpenIdConnectConstants.Claims.Subject, userId));
                if (additionalClaims != null)
                    claims.AddRange(additionalClaims);
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), Policies.CookieAuthentication));
            }
            if (storeId != null)
            {
                context.SetStoreData(serviceProvider.GetService<StoreRepository>().FindStore(storeId, userId).GetAwaiter().GetResult());
            }
            var scope = (IServiceScopeFactory)serviceProvider.GetService(typeof(IServiceScopeFactory));
            var provider = scope.CreateScope().ServiceProvider;
            context.RequestServices = provider;

            var httpAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            httpAccessor.HttpContext = context;

            var controller = (T)ActivatorUtilities.CreateInstance(provider, typeof(T));

            controller.Url = new UrlHelperMock(new Uri(context.Request.GetCurrentUrl()));
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = context
            };
            return controller;
        }

        public static List<string> GetModelSateError(this Controller controller)
        {
            List<string> errors = new List<string>();
            foreach (var modelState in controller.ModelState.Values)
            {
                foreach (var modelError in modelState.Errors)
                {
                    errors.Add(modelError.ErrorMessage);
                }
            }
            return errors;
        }
    }

    class UrlHelperMock : IUrlHelper
    {
        Uri _BaseUrl;
        public UrlHelperMock(Uri baseUrl)
        {
            _BaseUrl = baseUrl;
        }
        public ActionContext ActionContext => throw new NotImplementedException();

        public string Action(UrlActionContext actionContext)
        {
            return $"{_BaseUrl}mock";
        }

        public string Content(string contentPath)
        {
            return $"{_BaseUrl}{contentPath}";
        }

        public bool IsLocalUrl(string url)
        {
            return false;
        }

        public string Link(string routeName, object values)
        {
            return _BaseUrl.AbsoluteUri;
        }

        public string RouteUrl(UrlRouteContext routeContext)
        {
            return _BaseUrl.AbsoluteUri;
        }
    }
}
