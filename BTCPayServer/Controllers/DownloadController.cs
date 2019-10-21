using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Storage.Services;
using BTCPayServer.Storage.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace BTCPayServer.Controllers
{
    [Route("[controller]")]
    public class DownloadController : Controller
    {
        private readonly FileService _filesService;

        public DownloadController(FileService filesService)
        {
            _filesService = filesService;
        }

        [HttpGet]
        [Route("{fileId}")]
        public async Task<IActionResult> Download(string fileId)
        {
            string filePath = await _filesService.GetFilePath(fileId, User);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(filePath), new FileInfo(filePath).Name);
        }

        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (!provider.TryGetContentType(path, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
