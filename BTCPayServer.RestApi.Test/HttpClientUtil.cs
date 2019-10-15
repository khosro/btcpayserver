using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.RestApi.Test
{
    public class HttpClientUtil
    {
        static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };
            return new HttpClient(handler);
        }

        public static async Task<ApiResponse> SendAuthenticatedRequest(string url, HttpMethod httpMethod, HttpContent httpContent)
        {
            var request = await LoginAndCreateRequest(url, httpMethod);
            request.Content = httpContent;
            return await SendRequest(request);
        }

        public static async Task<ApiResponse> SendUnAuthenticatedRequest(string url, HttpMethod httpMethod, HttpContent httpContent)
        {
            var request = new HttpRequestMessage(httpMethod, url)
            {
                Content = httpContent
            };
            return await SendRequest(request);
        }

        public static async Task<ApiResponse> SendRequestByToken(string token, string url, HttpMethod httpMethod)
        {
            return await SendRequest(GetAuthentictedRequest(token, httpMethod, url));
        }

        static async Task<HttpRequestMessage> LoginAndCreateRequest(string url, HttpMethod httpMethod)
        {
            RegisterLogin registerLogin = new RegisterLogin();
            var usernamePassword = await registerLogin.CreateAccountAsync();
            await registerLogin.GetTokenAsync(usernamePassword.UserName, usernamePassword.Password);
            var request = GetAuthentictedRequest(registerLogin.Token, httpMethod, url);
            return request;
        }

        static HttpRequestMessage GetAuthentictedRequest(string token, HttpMethod httpMethod, string url)
        {
            var request = new HttpRequestMessage(httpMethod, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        static async Task<ApiResponse> SendRequest(HttpRequestMessage request)
        {
            var response = await GetHttpClient().SendAsync(request, HttpCompletionOption.ResponseContentRead);
            var data = await response.Content.ReadAsStringAsync();
            JObject payload = new JObject();
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                payload = JObject.Parse(data);
                Utility.Error(payload, response.StatusCode);
                Console.WriteLine($"Response : {payload.ToString()}");
            }
            else
            {
                Console.WriteLine($"Response : {data}");
            }

            var apiResponse = new ApiResponse { StatusCode = response.StatusCode, Payload = payload };
            return apiResponse;
        }

        public class ApiResponse
        {
            public JObject Payload { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }

        static void NEVER_EAT_POISON_Disable_CertificateValidation()
        {
            // Disabling certificate validation can expose you to a man-in-the-middle attack
            // which may allow your encrypted message to be read by an attacker
            // https://stackoverflow.com/a/14907718/740639
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
        }
    }
}
