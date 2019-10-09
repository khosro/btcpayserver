using System;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Validation;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/[controller]")]
    [ApiController]
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
    }
}
