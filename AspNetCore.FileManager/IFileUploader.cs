using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.FileManager
{
    public interface IFileUploader
    {
        Task<string> Upload(IFormFile file);
        void CheckFileFormat(IFormFile file);
    }
}
