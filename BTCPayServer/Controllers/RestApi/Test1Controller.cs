using System;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    public class Test1Controller : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StoreRepository _storeRepository;
        private readonly IServiceProvider _serviceProvider;

        public Test1Controller(UserManager<ApplicationUser> userManager, StoreRepository storeRepository, IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _storeRepository = storeRepository;
            this._serviceProvider = serviceProvider;
        }

        [HttpGet("Testaction")]
        public string GetCurrentUserId()
        {
            return "Test";
        }

        [HttpGet("Register")]
        public async Task<string> Register()
        {
            var account = (AccountController)_serviceProvider.GetService(typeof(AccountController));
            var RegisterDetails = new RegisterViewModel()
            {
                Email = Guid.NewGuid() + "@toto.com",
                ConfirmPassword = "Kitten0@",
                Password = "Kitten0@",
            };
            await account.Register(RegisterDetails);
            var UserId = account.RegisteredUserId;
            return UserId;
        }
    }
}
