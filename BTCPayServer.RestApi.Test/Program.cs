using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        //static string clientId = "c5e59a05-c3aa-413e-a57d-4756582b59eb";
        static string clientId = "ee3134a1-b3c8-4adf-8776-cd5173d28a4e";
        static string client_secret = "secret";
        const string email = "a@a.com", password = "123456";
        static string token = "";
        static string refresh_token = "";
        static HttpClient client = new HttpClient();
        static HttpStatusCode StatusCode;
        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            try
            {
                await GetResourceAsync();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            await GetTokenAsync();

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

        static async Task GetTokenAsync()
        {
            var t = await GetTokenAsync(client, email, password);
            Log(t);
        }

        private static async Task GetResourceAsync()
        {
            var resource = await GetResourceAsync(client);
            Log("API response: {0}", resource);
        }

        public static async Task CreateAccountAsync(HttpClient client, string email, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:58795/Account/Register")
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { email, password }), Encoding.UTF8, "application/json")
            };

            // Ignore 409 responses, as they indicate that the account already exists.
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return;
            }
            response.EnsureSuccessStatusCode();
        }

        public static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseurl}/connect/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = password,
                ["client_id"] = clientId,
                ["client_secret"] = client_secret,
                ["scope"] = "openid offline_access",
            });

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (payload["error"] != null)
            {
                throw new InvalidOperationException("An error occurred while retrieving an access token.");
            }
            token = (string)payload["access_token"];
            refresh_token = (string)payload["refresh_token"];
            return payload.ToString();
        }

        public static async Task<string> GetResourceAsync(HttpClient client)
        {
            string url = $"{baseurl}/api/test/me/id";
            url = $"{baseurl}/api/test1/Testaction";
            //url = $"{baseurl}/api/test1/Register";
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

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseurl}/connect/token");
            request.Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
               new KeyValuePair<string, string>("grant_type","refresh_token"),
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", client_secret),
                        new KeyValuePair<string, string>("refresh_token", refresh_token),
                        new KeyValuePair<string, string>("redirect_uri", "")
            });

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = (string)payload["access_token"];
            var refresh_tokenNew = (string)payload["refresh_token"];

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
