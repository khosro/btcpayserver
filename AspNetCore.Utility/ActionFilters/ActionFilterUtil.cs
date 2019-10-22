using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ActionFilterUtil
    {
        public static bool ValidationError(FilterContext context)
        {
            bool isValid = true;
            ResultExecutingContext resultExecutingContext = null;
            ActionExecutingContext actionExecutingContext = null;
            if (context is ResultExecutingContext)
            {
                resultExecutingContext = (ResultExecutingContext)context;
            }
            else if (context is ActionExecutingContext)
            {
                actionExecutingContext = (ActionExecutingContext)context;
            }
            if (!context.ModelState.IsValid)
            {
                isValid = false;
                var response = new SingleResponse<string>();
                response.ErrorMessages = context.ModelState.GetModelSateErrorList();
                var responseObject = response.ToHttpResponse();
                if (resultExecutingContext != null)
                {
                    resultExecutingContext.Result = responseObject;
                }
                else if (actionExecutingContext != null)
                {
                    actionExecutingContext.Result = responseObject;
                }
            }
            return isValid;
        }
    }
}
