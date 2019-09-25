using System;
using System.Collections.Generic;

namespace BTCPayServer.UiUtil
{
    public class CryptoControllerActionUtil
    {
        static Dictionary<string, string> crpytoCode2WalletSendActionForWalletController = new Dictionary<string, string>() { { "eth", "EthWalletSend" } };
        static Dictionary<string, string> crpytoCode2WalletTransactionsActionForWalletController = new Dictionary<string, string>() { { "eth", "EthWalletTransactions" } };
        
        public static string GetWalletSendActionByViewContext(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext)
        {
            return GetWalletActionByViewContext(viewContext, crpytoCode2WalletSendActionForWalletController);
        }

        public static string GetWalletTransactionsActionByViewContext(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext)
        {
            return GetWalletActionByViewContext(viewContext, crpytoCode2WalletTransactionsActionForWalletController);
        }

        static string GetWalletActionByViewContext(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, Dictionary<string, string> crpytoCode2WalletAction)
        {
            string action = "";
            try
            {
                object walletIdQueryString = null;

                viewContext.RouteData.Values.TryGetValue("walletid", out walletIdQueryString);
                if (walletIdQueryString != null && !string.IsNullOrWhiteSpace(walletIdQueryString.ToString()))
                {
                    action = GetWalletActionByWalletIdQueryString(walletIdQueryString.ToString(), crpytoCode2WalletAction);
                }
            }
            catch (Exception) { }
            return action;
        }


        static string GetWalletActionByWalletIdQueryString(string walletIdQueryString, Dictionary<string, string> crpytoCode2WalletAction)
        {
            string action = "";
            int cryptoCodeIndex = walletIdQueryString.LastIndexOf("-", StringComparison.InvariantCulture) + 1;
            var cryptoCode = walletIdQueryString.Substring(cryptoCodeIndex, walletIdQueryString.Length - cryptoCodeIndex);
            action = GetWalletActionByCryptoCode(cryptoCode, crpytoCode2WalletAction);

            return action;
        }

        static string GetWalletActionByCryptoCode(string cryptoCode, Dictionary<string, string> crpytoCode2WalletAction)
        {
            if (string.IsNullOrWhiteSpace(cryptoCode))
            {
                return "";
            }
            cryptoCode = cryptoCode.ToLowerInvariant().Trim();
            string action = "";
            crpytoCode2WalletAction.TryGetValue(cryptoCode, out action);
            return action;
        }
    }
}
