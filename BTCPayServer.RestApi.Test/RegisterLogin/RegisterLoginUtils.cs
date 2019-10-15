using System;
using System.Collections.Generic;
using System.Text;

namespace BTCPayServer.RestApi.Test
{
    public class GrantTypes
    {
        public const string Password = "password";
        public const string RefreshToken = "refresh_token";
    }

    public class Scopes
    {
        public const string ScopesGetToken = "openid offline_access";
    }
    public class Fields
    {
        public const string grant_type = "grant_type";
        public const string username = "username";
        public const string password = "password";
        public const string client_id = "client_id";
        public const string client_secret = "client_secret";
        public const string scope = "scope";
        public const string refresh_token = "refresh_token";
        public const string redirect_uri = "redirect_uri";
        public const string access_token = "access_token";
        public const string model = "model";
    }
}
