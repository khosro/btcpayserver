using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
namespace AspNetCore
{
    public static class ControllersUtils
    {

        public delegate void ArbitraryFunction(object[] objects, HttpContext context, IServiceProvider serviceProvider, string userId);
        public delegate void Security(HttpContext context, Claim[] additionalClaims, string userId);

        public static T GetController<T>(this HttpContext context, IServiceProvider serviceProvider, string userId = null, Claim[] additionalClaims = null, Security security = null,
            object[] objects = null, ArbitraryFunction arbitraryFunction = null) where T : Controller
        {
            if (objects != null)
            {
                objects = objects.Where(t => t != null).ToArray();
            }

            if (security != null)
                security(context, additionalClaims, userId);
            if (arbitraryFunction != null)
                arbitraryFunction(objects, context, serviceProvider, userId);

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

        #region ModelSate

        public static List<string> GetModelSateErrorList(this ControllerBase controller)
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

        public static string GetModelSateErrors(this ControllerBase controller)
        {
            var errors = GetModelSateErrorList(controller);
            return string.Join(",", errors);
        }

        public static List<string> GetModelSateErrorList(this ModelStateDictionary ModelState)
        {
            List<string> errors = new List<string>();
            foreach (var modelState in ModelState.Values)
            {
                foreach (var modelError in modelState.Errors)
                {
                    errors.Add(modelError.ErrorMessage);
                }
            }
            return errors;
        }

        public static string GetModelSateErrors(this ModelStateDictionary ModelState)
        {
            var errors = GetModelSateErrorList(ModelState);
            return string.Join(",", errors);
        }

        #endregion

        #region Urls
        public static string GetCurrentUrl(this HttpRequest request)
        {
            return string.Concat(
                        request.Scheme,
                        "://",
                        request.Host.ToUriComponent(),
                        request.PathBase.ToUriComponent(),
                        request.Path.ToUriComponent());
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
        #endregion Urls
    }
}
