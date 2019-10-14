using System;
using System.Collections.Generic;
using System.Text;

namespace BTCPayServer
{
    public class TokenResponseModel
    {
        public string error { get; set; }
        public string error_description { get; set; }
        public string token_type { get; set; }
        public string access_token { get; set; }
        public long? expires_in { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
    }
}
