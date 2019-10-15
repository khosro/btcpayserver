using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.FileManager.Models;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.FileManager
{
    public interface IFileUploader
    {
        Task<string> Upload(FileUploadModel file);
        bool IsFileFormatAcceptable(IFormFile file);
    }
}
