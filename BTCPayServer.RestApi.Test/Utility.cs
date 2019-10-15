using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.RestApi.Test
{
    public class Utility
    {
        public static string baseurl = "http://localhost:5000/api/v1";
        //public static string baseurl = "https://192.168.41.93/api/v1";

        //public static string clientId = "864e4d8d-c3eb-4ac6-a225-e09862ca94c7";//Test Server
        //public static string clientId = "680f2454-7df3-4950-9f63-b39dd44bb0aa";//My computer
        public static string clientId = "2224adb0-481f-4bdb-844a-cc8eacbd3199";//My home computer

        #region Const
        const string fileSeparator = "file:";
        public const string status = "hasError";
        public const string error = "errorMessage";
        public const string errorServer = "serverErrorMessage";
        public static string client_secret = "secret";
        #endregion

        public static string GetRootDir()
        {
            var index = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Directory.FullName.LastIndexOf(fileSeparator);
            var rootDir = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Directory.FullName.Substring(index + fileSeparator.Length + 1);
            return rootDir;
        }

        public static void Error(JObject payload, HttpStatusCode statusCode)
        {
            var errorObj = payload[error];
            var errorServerObj = payload[errorServer];
            if ((errorObj != null && !string.IsNullOrEmpty(errorObj.ToString())) || (errorServerObj != null && !string.IsNullOrEmpty(errorServerObj.ToString())))
            {
                string fullError = payload[error].ToString() + " " + payload[errorServer].ToString() + statusCode;
                throw new InvalidOperationException(fullError);
            }
            else if (errorObj == null || errorServerObj == null)
            {
                throw new InvalidOperationException(payload.ToString());
            }
        }

        #region Logging
        public static void Log(string message = "")
        {
            Log(message, null);
        }
        public static void Log(string format, object arg0 = null)
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
        #endregion
    }
}
