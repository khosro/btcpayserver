using System;
using System.Collections.Generic;
using System.IO;
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
    public class UserNameAndPassword
    {
        public static UserNameAndPassword Create()
        {
            return new UserNameAndPassword() { Password = "123456", UserName = Guid.NewGuid().ToString() + "@gmail.com" };
        }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    class RegisterLogin
    {
        HttpStatusCode StatusCode;

        #region const
        string BTCPayServer_RestApi_Test_Url_Location = "C:/BTCPayServer.RestApi.Test.Url.txt";
        public string Token { get; private set; }
        string refresh_token = "";

        string ApiTokenUrl = Utility.baseurl + "/authenticate/connect/token";
        string RegisterUrl = $"{Utility.baseurl}/authenticate/register";
        string TestProtectedUrl = $"{Utility.baseurl}/test1/testaction";

        #endregion

        public RegisterLogin()
        {
            if (File.Exists(BTCPayServer_RestApi_Test_Url_Location))
            {
                Utility.baseurl = File.ReadAllText(BTCPayServer_RestApi_Test_Url_Location).Trim();
            }
        }

        public async Task RunTest(string[] args)
        {
            try
            {
                await GetResourceAsync();
            }
            catch (Exception ex)
            {
                Utility.Log("Get resource without login " + ex.Message);
            }

            var userNameAndPassword = UserNameAndPassword.Create();
            await CreateAccountAsync(userNameAndPassword.UserName, userNameAndPassword.Password);

            await GetTokenAsync(userNameAndPassword.UserName, userNameAndPassword.Password);

            await GetResourceAsync();

            int elapsed = 0;
            for (int i = 0; i < 30; i++)
            {
                Utility.Log($"Iterate number {i} elapsed seconds { elapsed }");
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
                    Utility.Log($" Error in Iterate {i} {ex.Message}");
                    if (StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await RefreshTokenAsync();
                    }
                }
            }

            Console.ReadLine();
        }

        public async Task<UserNameAndPassword> CreateAccountAsync()
        {
            var userNameAndPassword = UserNameAndPassword.Create();
            await CreateAccountAsync(userNameAndPassword.UserName, userNameAndPassword.Password);
            return userNameAndPassword;
        }

        public async Task CreateAccountAsync(string email, string password)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new { email = email, password = password }), Encoding.UTF8, "application/json");
            var apiResponse = await HttpClientUtil.SendUnAuthenticatedRequest(RegisterUrl, HttpMethod.Post, content);
            StatusCode = apiResponse.StatusCode;
        }

        public async Task<string> GetTokenAsync(string email, string password)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiTokenUrl);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Fields.grant_type] = GrantTypes.Password,
                [Fields.username] = email,
                [Fields.password] = password,
                [Fields.client_id] = Utility.clientId,
                [Fields.client_secret] = Utility.client_secret,
                [Fields.scope] = Scopes.ScopesGetToken,
            });

            var apiResponse = await HttpClientUtil.SendUnAuthenticatedRequest(ApiTokenUrl, HttpMethod.Post, content);
            StatusCode = apiResponse.StatusCode;
            var payload = apiResponse.Payload;
            Token = GetAccessToken(payload);
            refresh_token = GetRefreshToken(payload);
            return payload.ToString();
        }

        async Task RefreshTokenAsync()
        {
            Utility.Log("Get new token by refresh token");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiTokenUrl);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Fields.grant_type] = GrantTypes.RefreshToken,
                [Fields.client_id] = Utility.clientId,
                [Fields.client_secret] = Utility.client_secret,
                [Fields.refresh_token] = refresh_token,
                [Fields.redirect_uri] = "",
            });

            var apiResponse = await HttpClientUtil.SendUnAuthenticatedRequest(ApiTokenUrl, HttpMethod.Post, content);
            StatusCode = apiResponse.StatusCode;
            var payload = apiResponse.Payload;

            Token = GetAccessToken(payload);
            var refresh_tokenNew = GetRefreshToken(payload);

            if (refresh_tokenNew.Equals(refresh_token))
            {
                Utility.Log($"Refresh token is the same as before");
            }
            else
            {
                refresh_token = refresh_tokenNew;
                Utility.Log($"Refresh token are not the same as before");
            }
        }

        static string GetAccessToken(JObject payload)
        {
            return (string)payload[Fields.model][Fields.access_token];
        }
        static string GetRefreshToken(JObject payload)
        {
            return (string)payload[Fields.model][Fields.refresh_token];
        }
        async Task GetResourceAsync()
        {
            var apiResponse = await HttpClientUtil.SendRequestByToken(Token, TestProtectedUrl, HttpMethod.Get);
            StatusCode = apiResponse.StatusCode;
            Utility.Log("GetResourceAsync API response: {0}", apiResponse.Payload);
        }
    }
}
