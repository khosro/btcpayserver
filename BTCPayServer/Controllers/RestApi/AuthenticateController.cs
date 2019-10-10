using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Validation;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StoreRepository _storeRepository;
        private readonly IServiceProvider _serviceProvider;

        public AuthenticateController(UserManager<ApplicationUser> userManager, StoreRepository storeRepository, IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _storeRepository = storeRepository;
            this._serviceProvider = serviceProvider;
        }

        [HttpPost("register")]
        public async Task<object> Register([FromBody]JObject data)
        {
            try
            {
                string email = data["email"]?.ToObject<string>();
                string password = data["password"]?.ToObject<string>();
                var account = ControllersUtil.GetController<AccountController>(this.HttpContext, _serviceProvider);
                var RegisterDetails = new RegisterViewModel()
                {
                    Email = email,
                    ConfirmPassword = password,
                    Password = password,
                };
                var model = await account.Register(RegisterDetails);
                string errors = string.Join(",", account.GetModelSateError()).Trim();
                bool hasError = !string.IsNullOrWhiteSpace(errors);
                if (string.IsNullOrWhiteSpace(account.RegisteredUserId) && !hasError)
                    return new { Status = false, Error = "You can not sign in"/*Maybe policies.LockSubscription = true*/ };
                else
                    return new { Status = hasError ? false : true, Error = errors };
            }
            catch (Exception ex)
            {
                //TODO.Log Error 
                return new { Status = false, Error = "Error" };
            }
        }

        [HttpPost("connect/token")]
        public async Task<object> Token([FromForm] TokenModel data)
        {
            /*
             * TODO.
             * This is bad approach.Solve it.
             * But in order to prevent the following error, i do it.
             * 
             * "has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
             * No 'Access-Control-Allow-Origin' header is present on the requested resource."
             
             * And also test the following but does not work(Apply CORS Globally)
             * 
                services.Configure<MvcOptions>(options =>
                {
                options.Filters.Add(new CorsAuthorizationFilterFactory("AllowMyOrigin"));
                });
             * 
             */
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;//Because this is our host we can ignore it.
            };
            HttpClient httpClient = new HttpClient(handler);

            /*
             * For exmaple in Server maybe IP of that server can not be resolved, we do not use it.
             * var uri = new Uri(new Uri(this.HttpContext.Request.GetAbsoluteRootUri().ToString()), "connect/token");
             */

            /*NOTICE : The mapped port in firewall must be the same as port in local server
             * For example in firewall when we must mapped as following   77.77.77.77:8080 -> 192.168.1.2:8080
             * Not following  77.77.77.77:8081 -> 192.168.1.2:8080(For exmaple something like we do with RDP connection, that we changed port to connect from outside)
             * */
            var uri = new Uri($"{this.HttpContext.Request.Scheme}://localhost:{this.HttpContext.Request.Host.Port}/connect/token");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                        {
                            new KeyValuePair<string, string>("grant_type", data.grant_type),
                            new KeyValuePair<string, string>("client_id", data.client_id ),
                            new KeyValuePair<string, string>("client_secret",  data.client_secret ),
                            new KeyValuePair<string, string>("refresh_token", data.refresh_token ),
                            new KeyValuePair<string, string>("redirect_uri",  data.redirect_uri ),
                            new KeyValuePair<string, string>("username",  data.username ),
                            new KeyValuePair<string, string>("password",  data.password ),
                            new KeyValuePair<string, string>("scope",   data.scope),
                        })
            };
            var response = await httpClient.SendAsync(httpRequest);
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }

        #region Obsolete
        [HttpPost("connect/tokenjson")]
        [Obsolete("Use Token instead")]
        public async Task<object> Tokenjson([FromBody]JObject data)
        {
            /*
             * TODO.
             * This is bad approach.Solve it.
             * But in order to prevent the following error, i do it.
             * 
             * "has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
             * No 'Access-Control-Allow-Origin' header is present on the requested resource."
             
             * And also test the following but does not work(Apply CORS Globally)
             * 
                services.Configure<MvcOptions>(options =>
                {
                options.Filters.Add(new CorsAuthorizationFilterFactory("AllowMyOrigin"));
                });
             * 
             */
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;//Because this is our host we can ignore it.
            };
            HttpClient httpClient = new HttpClient(handler);

            /*
                 * For exmaple in Server maybe IP of that server can not be resolved, we do not use it.
                 * var uri = new Uri(new Uri(this.HttpContext.Request.GetAbsoluteRootUri().ToString()), "connect/token");
                 */
            /*NOTICE : The mapped port in firewall must be the same as port in local server
            * For example in firewall when we must mapped as following   77.77.77.77:8080 -> 192.168.1.2:8080
            * Not following  77.77.77.77:8081 -> 192.168.1.2:8080(For exmaple something like we do with RDP connection, that we changed port to connect from outside)
            * */
            var uri = new Uri($"{this.HttpContext.Request.Scheme}://localhost:{this.HttpContext.Request.Host.Port}/connect/token");
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("grant_type", data["grant_type"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("client_id",data["client_id"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("client_secret", data["client_secret"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("refresh_token", data["refresh_token"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("redirect_uri", data["redirect_uri"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("username", data["username"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("password", data["password"]?.ToObject<string>()),
                        new KeyValuePair<string, string>("scope", data["scope"]?.ToObject<string>()),
                    })
            };

            var response = await httpClient.SendAsync(httpRequest);
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }
        #endregion
    }

    public class TokenModel
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string refresh_token { get; set; }
        public string redirect_uri { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string scope { get; set; }
    }
}
