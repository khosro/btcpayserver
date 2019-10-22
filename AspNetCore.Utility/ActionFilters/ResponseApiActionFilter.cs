using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ResponseApiActionFilter : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (ActionFilterUtil.ValidationError(context))
            {
                var response = new SingleResponse<object>();
                object result = context.Result;
                if (context.Result is Microsoft.AspNetCore.Mvc.ObjectResult objectResult)
                {
                    result = objectResult.Value;
                }
                response.Model = result;
                context.Result = response.ToHttpResponse();
            }
            base.OnResultExecuting(context);
        }
    }
}
