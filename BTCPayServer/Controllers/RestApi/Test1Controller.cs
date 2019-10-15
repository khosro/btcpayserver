using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.FileManager;
using BTCPayServer.Data;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Validation;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace BTCPayServer.Controllers.RestApi
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors(CorsPolicies.All)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    public class Test1Controller : ControllerBase
    {
        ImageUploader _imageUploader;
        public Test1Controller(ImageUploader imageUploader)
        {
            _imageUploader = imageUploader;
        }

        [HttpGet("testaction")]
        public IActionResult Testaction()
        {
            var response = new SingleResponse<string>();
            response.Model = "test";
            return response.ToHttpResponse();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(/*IEnumerable<IFormFile> files[FromBody]Payload data*/[FromForm] Payload data)
        {
            IEnumerable<IFormFile> files = HttpContext.Request.Form.Files;
            StringValues values;
            HttpContext.Request.Form.TryGetValue("data", out values);
            var response = new SingleResponse<string>();
            await _imageUploader.Upload(new AspNetCore.FileManager.Models.FileUploadModel() { File = files.FirstOrDefault(), Path = "test" });
            response.Model = "test";
            return response.ToHttpResponse();
        }
    }

    public class Payload
    {
        public Data data { get; set; }
        public IEnumerable<IFormFile> files { get; set; }
    }


    public class Data
    {
        public string FirstName { get; set; }
    }
}
