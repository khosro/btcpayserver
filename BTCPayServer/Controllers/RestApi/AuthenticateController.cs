﻿using System;
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
                string email = data["email"].ToObject<string>();
                string password = data["password"].ToObject<string>();
                var account = ControllersUtil.GetController<AccountController>(this.HttpContext, _serviceProvider);
                var RegisterDetails = new RegisterViewModel()
                {
                    Email = email,
                    ConfirmPassword = password,
                    Password = password,
                };
                var model = await account.Register(RegisterDetails);
                return new { Status = true, Error = string.Join(",", account.GetModelSateError()).Trim() };
            }
            catch (Exception ex)
            {
                //TODO.Log Error 
                return new { Status = false, Error = "Error" };
            }
        }

        [HttpPost("connect/token")]
        public async Task<object> Token([FromBody]JObject data)
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
            HttpClient httpClient = new HttpClient();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(this.HttpContext.Request.GetAbsoluteRootUri().ToString()), "connect/token"))
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
    }
}
