using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.FileManager.Models;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.FileManager
{
    public class ImageUploader : IFileUploader
    {
        FileServerConfig _fileServerConfig;
        public ImageUploader(FileServerConfig fileServerConfig)
        {
            this._fileServerConfig = fileServerConfig;
        }

        public bool IsFileFormatAcceptable(IFormFile file)
        {
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                fileBytes = ms.ToArray();
            }

            var isFormatsupported = GetImageFormat(fileBytes) != ImageFormat.unknown;
            if (!isFormatsupported)
            {
                throw new InvalidOperationException("Invalid file format");
            }
            return true;
        }

        public async Task<string> Upload(FileUploadModel fileUploadModel)
        {
            string fileName;
            IFormFile file = fileUploadModel.File;
            try
            {
                IsFileFormatAcceptable(file);
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                fileName = Guid.NewGuid().ToString() + extension;

                //var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", fileName);

                string filePath = this._fileServerConfig.UploadFolder;
                if (!string.IsNullOrWhiteSpace(fileUploadModel.Path))
                {
                    filePath = Path.Combine(filePath, fileUploadModel.Path);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                }
                var path = Path.Combine(filePath, fileName);

                using (var bits = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(bits);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return fileName;
        }


        #region Util methods

        public enum ImageFormat
        {
            bmp,
            jpeg,
            gif,
            tiff,
            png,
            unknown
        }

        public static ImageFormat GetImageFormat(byte[] bytes)
        {
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
                return ImageFormat.bmp;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat.gif;

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat.png;

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
                return ImageFormat.tiff;

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
                return ImageFormat.tiff;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat.jpeg;

            return ImageFormat.unknown;
        }

        #endregion
    }
}
