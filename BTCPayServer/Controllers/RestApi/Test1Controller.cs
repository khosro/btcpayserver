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
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation;
using System.Linq;
using Microsoft.Extensions.Primitives;
using AspNetCore;
using System.ComponentModel.DataAnnotations;
using BTCPayServer.Storage.Services;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Controllers.RestApi
{
    [Route(ControllersUtil.ApiBersion1BaseUrl)]
    [EnableCors(CorsPolicies.All)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
    public class Test1Controller : ApiControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ImageUploader _imageUploader;
        private readonly FileService _FileService;
        private readonly UserManager<ApplicationUser> _userManager;
        public Test1Controller(ImageUploader imageUploader, FileService fileService, UserManager<ApplicationUser> userManager, IServiceProvider serviceProvider)
        {
            _imageUploader = imageUploader;
            _FileService = fileService;
            _userManager = userManager;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("testaction")]
        public string Testaction()
        {
            return "Test OK";
        }

        [HttpPost("upload")]
        public async Task<string> UploadFile(
            [ModelBinder(BinderType = typeof(JsonWithFilesFormDataModelBinder))]
            [FromForm] Data data)
        {
            string fileNames = string.Join(",", data.Files.Select(t => t.FileName));

            var file = data.Files.FirstOrDefault(t => "files".Equals(t.Name, StringComparison.InvariantCulture));
            //  var file = data.files;
            /*Another way to get files
             * IEnumerable<IFormFile> files = HttpContext.Request.Form.Files;
             * 
             * */
            /*Another way to get form data values
             * StringValues values;
            HttpContext.Request.Form.TryGetValue("data", out values);
            */
            string responseText = "";
            if (file != null)
            {
                //await _imageUploader.Upload(new AspNetCore.FileManager.Models.FileUploadModel() { File = file, Path = "test" });
                var storedFile = await _FileService.AddFile(file, _userManager.GetUserId(User));
                var imageAddress = await _FileService.GetFileUrl(Request.GetAbsoluteRootUri(), storedFile.Id, User);
                responseText = $"Number of sent files are {data.Files.Count()} and file names are {fileNames} and only file {file.FileName} uploaded, url {imageAddress}";
            }
            else
            {
                responseText = "There is not any file to upload";
            }
            responseText += $" FirstName : {data.FirstName}";
            return responseText;
        }
    }
    public class Data
    {
        public Data()
        {
            Files = new List<IFormFile>();
        }
        [Required]
        public string FirstName { get; set; }
        public IEnumerable<IFormFile> Files { get; set; }
    }
}
