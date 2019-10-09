//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Text;

//namespace BTCPayServer.RestApi.Test
//{
//    public interface IHttpClientCreator
//    {
//        System.Net.Http.HttpClient Create();
//    }

//    public class MyWin81Factory : IHttpClientCreator
//    {
//        public System.Net.Http.HttpClient Create()
//        {
//            var filter = new HttpBaseProtocolFilter(); // do something with this
//            filter.IgnorableServerCertificateErrors.Add(...)
//            var client = new System.Net.Http.HttpClient(new WinRtHttpClientHandler(filter));
//            return client;
//        }
//    }

//    public class MyNetFxFactory : IHttpClientCreator
//    {
//        static MyNetFxFactory()
//        {
//            ServicePointManager.ServerCertificateValidationCallback = ...
//    }

//        public HttpClient Create()
//        {
//            var client = new HttpClient();
//        }
//    }
//}
