using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.RestApi.Test
{
    class Program
    {
        static string baseurl = "http://localhost:5000";
        //static string baseurl = "https://192.168.41.93";
        static string BTCPayServer_RestApi_Test_Url_Location = "C:/BTCPayServer.RestApi.Test.Url5555.txt";

        //static string clientId = "ee3134a1-b3c8-4adf-8776-cd5173d28a4e";//My computer
        //static string clientId = "864e4d8d-c3eb-4ac6-a225-e09862ca94c7";//Test Server
        static string clientId = "2224adb0-481f-4bdb-844a-cc8eacbd3199";//my home Server

        static string ApiToken = baseurl + "/api/authenticate/connect/token";//my home Server


        static string client_secret = "secret";
        static string token = "";
        static string refresh_token = "";
        static HttpClient client;
        static HttpStatusCode StatusCode;
        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();


        static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };

            var client = new HttpClient(handler);

            return client;
        }

        static void NEVER_EAT_POISON_Disable_CertificateValidation()
        {
            // Disabling certificate validation can expose you to a man-in-the-middle attack
            // which may allow your encrypted message to be read by an attacker
            // https://stackoverflow.com/a/14907718/740639
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (
                    object s,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors
                )
                {
                    return true;
                };
        }


        public static async Task MainAsync(string[] args)
        {
            if (File.Exists(BTCPayServer_RestApi_Test_Url_Location))
            {
                baseurl = File.ReadAllText(BTCPayServer_RestApi_Test_Url_Location).Trim();
            }
            client = GetHttpClient();
            try
            {
                await GetResourceAsync();
            }
            catch (Exception ex)
            {
                Log("Get resource without login " + ex.Message);
            }

            NEVER_EAT_POISON_Disable_CertificateValidation();

            string email = Guid.NewGuid().ToString() + "@yahoo.com";
            //email = "test";
            string password = "1234567";

            await CreateAccountAsync(email, password);

            await GetTokenAsync(email, password);

            await GetResourceAsync();

            await RefreshTokenAsync();//This is for test remove it.

            int elapsed = 0;
            for (int i = 0; i < 30; i++)
            {
                Log($"Iterate number {i} elapsed seconds { elapsed }");
                if (i == 0)
                {
                    int second = 8;
                    Thread.Sleep(TimeSpan.FromSeconds(second));
                    elapsed += second;
                }
                else
                {
                    int second = 3;
                    Thread.Sleep(TimeSpan.FromSeconds(second));
                    elapsed += second;
                }
                try
                {
                    await GetResourceAsync();
                }
                catch (Exception ex)
                {
                    Log($" Error in Iterate {i} {ex.Message}");
                    if (StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await RefreshTokenAsync();
                    }
                }
            }

            Console.ReadLine();
        }

        static void Log(string message = "")
        {
            Log(message, null);
        }

        static void Log(string format, object arg0 = null)
        {
            if (arg0 != null)
            {
                Console.WriteLine(string.Format(format, arg0));
            }
            else
            {
                Console.WriteLine(format);
            }

            //Console.WriteLine("Token is {0}", token);
            Console.WriteLine("");
        }

        static async Task GetTokenAsync(string email, string password)
        {
            var t = await GetTokenAsync(client, email, password);
            Log(t);
        }

        private static async Task GetResourceAsync()
        {
            var resource = await GetResourceAsync(client);
            Log("GetResourceAsync API response: {0}", resource);
        }

        public static async Task CreateAccountAsync(string email, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseurl}/api/authenticate/register")
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { email = email, password = password }), Encoding.UTF8, "application/json")
            };

            ServicePointManager.ServerCertificateValidationCallback +=
       (sender, cert, chain, sslPolicyErrors) => true;

            // Ignore 409 responses, as they indicate that the account already exists.
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return;
            }
            response.EnsureSuccessStatusCode();

            string responsedata = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Login Response : {responsedata}");
        }

        public static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
        {
            //var request = new HttpRequestMessage(HttpMethod.Post, ApiToken);
            //request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            //{
            //    ["grant_type"] = "password",
            //    ["username"] = email,
            //    ["password"] = password,
            //    ["client_id"] = clientId,
            //    ["client_secret"] = client_secret,
            //    ["scope"] = "openid offline_access",
            //});

            var request = new HttpRequestMessage(HttpMethod.Post, ApiToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                grant_type = "password",
                username = email,
                password = password,
                client_id = clientId,
                client_secret = client_secret,
                scope = "openid offline_access"
            }), Encoding.UTF8, "application/json");


            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (payload["error"] != null)
            {
                throw new InvalidOperationException(payload["error"].ToString());
            }
            token = (string)payload["access_token"];
            refresh_token = (string)payload["refresh_token"];
            return payload.ToString();
        }

        public static async Task<string> GetResourceAsync(HttpClient client)
        {
            string url = $"{baseurl}/api/test1/testaction";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            StatusCode = response.StatusCode;
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        static async Task RefreshTokenAsync()
        {
            Log("Get new token by refresh token");

            /* var request = new HttpRequestMessage(HttpMethod.Post, ApiToken);
             request.Content = new FormUrlEncodedContent(
                 new Dictionary<string, string>
                 {
                     ["grant_type"] = "refresh_token",
                     ["client_id"] = clientId,
                     ["client_secret"] = client_secret,
                     ["refresh_token"] = refresh_token,
                     ["redirect_uri"] = "",
                 });
                 */

            var request = new HttpRequestMessage(HttpMethod.Post, ApiToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                grant_type = "refresh_token",
                client_id = clientId,
                client_secret = client_secret,
                refresh_token = refresh_token,
                redirect_uri = "",
            }), Encoding.UTF8, "application/json");


            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (payload["error"] != null)
            {
                throw new InvalidOperationException(payload["error"].ToString());
            }

            token = (string)payload["access_token"];
            var refresh_tokenNew = (string)payload["refresh_token"];

            Log($"RefreshTokenAsync  {payload.ToString()}");

            if (refresh_tokenNew.Equals(refresh_token))
            {
                Log($"Refresh token is the same as before");
            }
            else
            {
                refresh_token = refresh_tokenNew;
                Log($"Refresh token are not the same as before");
            }

            if (payload["error"] != null)
            {
                throw new InvalidOperationException("An error occurred while retrieving an access token.");
            }

        }
    }
}
