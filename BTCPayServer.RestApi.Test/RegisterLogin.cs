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
    class RegisterLogin
    {
        static string baseurl = "http://localhost:5000";
        //static string baseurl = "https://192.168.41.93";

        //static string clientId = "864e4d8d-c3eb-4ac6-a225-e09862ca94c7";//Test Server
        static string clientId = "680f2454-7df3-4950-9f63-b39dd44bb0aa";//My computer

        #region const
        static string BTCPayServer_RestApi_Test_Url_Location = "C:/BTCPayServer.RestApi.Test.Url.txt";
        //static string ApiToken = baseurl + "/connect/token";//my home Server
        static string ApiToken = baseurl + "/api/authenticate/connect/token";//my home Server
        static bool IsSendApiTokenDataAsJson = false;
        static string client_secret = "secret";
        static string token = "";
        static string refresh_token = "";
        static HttpClient client;
        static HttpStatusCode StatusCode;
        const string status = "hasError";
        const string error = "errorMessage";
        const string errorServer = "serverErrorMessage";
        #endregion

        static RegisterLogin()
        {
            client = GetHttpClient();
        }

        public static async Task LoginWithProfile()
        {
            string email = Guid.NewGuid().ToString() + "@gmail.com";
            // email = "sasas11";
            string password = "1234567";

            await CreateAccountAsync(email, password, true);
        }

        public static async Task Run(string[] args)
        {
            if (File.Exists(BTCPayServer_RestApi_Test_Url_Location))
            {
                baseurl = File.ReadAllText(BTCPayServer_RestApi_Test_Url_Location).Trim();
            }
            try
            {
                await GetResourceAsync();
            }
            catch (Exception ex)
            {
                Log("Get resource without login " + ex.Message);
            }

            string email = Guid.NewGuid().ToString() + "@yahoo.com";
            // email = "sasas11";
            string password = "1234567";

            await CreateAccountAsync(email, password);

            await GetTokenAsync(email, password);

            await GetResourceAsync();

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

        static async Task CreateAccountAsync(string email, string password, bool iswithProfile = false/*This is for test.Remove in future*/)
        {
            string url = "";
            if (iswithProfile)
            {
                url = $"{baseurl}/api/test1/registerprofile";
            }
            else
            {
                url = $"{baseurl}/api/authenticate/register";
            }
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { email = email, password = password }), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            // response.EnsureSuccessStatusCode();
            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            Error(payload, response.StatusCode);

            Console.WriteLine($"Login Response : {payload.ToString()}");
        }

        static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiToken);

            if (IsSendApiTokenDataAsJson)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    grant_type = "password",
                    username = email,
                    password = password,
                    client_id = clientId,
                    client_secret = client_secret,
                    scope = "openid offline_access"
                }), Encoding.UTF8, "application/json");
            }
            else
            {
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = email,
                    ["password"] = password,
                    ["client_id"] = clientId,
                    ["client_secret"] = client_secret,
                    ["scope"] = "openid offline_access",
                });
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            //response.EnsureSuccessStatusCode();
            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            Error(payload, response.StatusCode);
            token = (string)payload["access_token"];
            refresh_token = (string)payload["refresh_token"];
            return payload.ToString();
        }

        static async Task RefreshTokenAsync()
        {
            Log("Get new token by refresh token");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiToken);
            if (IsSendApiTokenDataAsJson)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    grant_type = "refresh_token",
                    client_id = clientId,
                    client_secret = client_secret,
                    refresh_token = refresh_token,
                    redirect_uri = "",
                }), Encoding.UTF8, "application/json");
            }
            else
            {
                request.Content = new FormUrlEncodedContent(
                          new Dictionary<string, string>
                          {
                              ["grant_type"] = "refresh_token",
                              ["client_id"] = clientId,
                              ["client_secret"] = client_secret,
                              ["refresh_token"] = refresh_token,
                              ["redirect_uri"] = "",
                          });
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            //response.EnsureSuccessStatusCode();
            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            Error(payload, response.StatusCode);

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

            if (!string.IsNullOrEmpty(payload[error].ToString()))
            {
                throw new InvalidOperationException(payload[error].ToString());
            }
        }

        static async Task<string> GetResourceAsync(HttpClient client)
        {
            string url = $"{baseurl}/api/test1/testaction";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            //response.EnsureSuccessStatusCode();
            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            Error(payload, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        #region Util Methods
        static void Error(JObject payload, HttpStatusCode statusCode)
        {
            StatusCode = statusCode;

            if (!string.IsNullOrEmpty(payload[error].ToString()) || !string.IsNullOrEmpty(payload[errorServer].ToString()))
            {
                string fullError = payload[error].ToString() + " " + payload[errorServer].ToString() + StatusCode;
                throw new InvalidOperationException(fullError);
            }
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
            Console.WriteLine("");
        }

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
        #endregion Util Methods
    }
}
