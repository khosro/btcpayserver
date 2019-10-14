using System;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Validation;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    public class Test1Controller : ControllerBase
    {
        public Test1Controller()
        { }

        [HttpGet("testaction")]
        public IActionResult GetCurrentUserId()
        {
            var response = new SingleResponse<string>();
            return response.ToHttpResponse();
        }
    }
}
