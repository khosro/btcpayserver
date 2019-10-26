using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation;
using System.Linq;
using Microsoft.Extensions.Primitives;
using AspNetCore;
using System.ComponentModel.DataAnnotations;
using BTCPayServer.Storage.Services;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using BTCPayServer.Data;

namespace BTCPayServer.Controllers.RestApi
{
    [Route(ControllersUtil.ApiBersion1BaseUrl)]
    [EnableCors(CorsPolicies.All)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    public class FileController : ApiControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FileService _FileService;
        private readonly UserManager<ApplicationUser> _userManager;
        public FileController(FileService fileService, UserManager<ApplicationUser> userManager, IServiceProvider serviceProvider)
        {
            _FileService = fileService;
            _userManager = userManager;
            _serviceProvider = serviceProvider;
        }

        [HttpPost("download")]
        public async Task<IActionResult> DownloadFile([FromBody]JObject data)
        {
            string fileid = data["fileid"]?.ToObject<string>();
            var userId = _userManager.GetUserId(User);
            var controller = HttpContext.GetControllerUtil<DownloadController>(_serviceProvider, userId);
            var file = await controller.Download(fileid);
            return file;
        }
    }
}
