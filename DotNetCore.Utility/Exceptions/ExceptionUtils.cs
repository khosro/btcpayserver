using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore
{
    public static class ExceptionUtils
    {
        public static string GetStackStrace(this Exception ex)
        {
            StringBuilder stackBuilder = new StringBuilder();
            stackBuilder.Append("\r\n");
            stackBuilder.Append(ex.Message);
            stackBuilder.Append("\r\n");
            stackBuilder.Append(ex.StackTrace);
            stackBuilder.Append("\r\n");
            if (ex.InnerException != null)
            {
                stackBuilder.Append("\r\n");
                stackBuilder.Append(GetStackStrace(ex.InnerException));
            }
            return stackBuilder.ToString();
        }
    }
}
