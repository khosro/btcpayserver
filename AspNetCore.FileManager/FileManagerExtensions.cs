using AspNetCore.Utility.Configs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace AspNetCore.FileManager
{
    public static class FileManagerExtensions
    {
        public static IServiceCollection AddFileServer(this IServiceCollection services)
        {
            services.AddSingleton(s => s.Config());
            services.AddTransient<ImageUploader>();
            //services.TryAddSingleton<FileServerConfig>(o =>            o.GetRequiredService<IOptions<FileServerConfig>>().Value);
            return services;
        }

        private static FileServerConfig Config(this IServiceProvider serviceProvider)
        {
            IConfiguration conf = serviceProvider.GetService<IConfiguration>();
            Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment = serviceProvider.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();

            var uploadfolder = conf.GetOrDefault<string>("uploadfolder", hostingEnvironment.WebRootPath);
            if (!Directory.Exists(uploadfolder))
            {
                Directory.CreateDirectory(uploadfolder);
            }
            return new FileServerConfig() { UploadFolder = uploadfolder };
        }
    }
}
