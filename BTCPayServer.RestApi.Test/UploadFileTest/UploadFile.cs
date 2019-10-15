using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BTCPayServer.RestApi.Test
{
    public class UploadFile
    {
        static string Url = Utility.baseurl + "/test1/upload";
        static string UploadFolder = "UploadFileTest";
        static string FileName = "Apple.jpg";

        public static async Task Upload()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(Utility.GetRootDir(), UploadFolder, FileName));

            ByteArrayContent bytes = new ByteArrayContent(data);
            MultipartFormDataContent multiContent = new MultipartFormDataContent();
            multiContent.Add(bytes, "files", FileName);

            //var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "FirstName", "Salam" } });
            var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                        {
                            new KeyValuePair<string, string>("FirstName", "Salam"),
                        });
            //multiContent.Add(content, "data");

            multiContent.Add(new StringContent("tttttttttt"), "FirstName");
            multiContent.Add(new StringContent(JsonConvert.SerializeObject(new Test() { FirstName= "2222"}), Encoding.UTF8, "application/json"), "data");


            /*var multiContent = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            multiContent.Add(new StreamContent(new MemoryStream(data)), "files", "upload.jpg");
            */

            await HttpClientUtil.SendAuthenticatedRequest(Url, HttpMethod.Post, multiContent);
        }
    }

    class Test
    {
        public string FirstName { get; set; }
    }
}
