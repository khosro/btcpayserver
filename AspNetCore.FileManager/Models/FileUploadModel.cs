using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.FileManager.Models
{
    public class FileUploadModel
    {
        public IFormFile File { get; set; }

        public string Path { get; set; }
    }
}
