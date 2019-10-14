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
        public static void Main(string[] args)
        {
            RegisterLoginRun(args);
        }

        static void RegisterLoginRun(string[] args)
        {
            RegisterLogin.Run(args).GetAwaiter().GetResult();

            //RegisterLogin.LoginWithProfile().GetAwaiter().GetResult();
        }
    }
}
