using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var response = new SingleResponse<string>();
                response.ErrorMessages = context.ModelState.GetModelSateErrorList();
                context.Result = response.ToHttpResponse();
            }
            base.OnActionExecuting(context);
        }
    }
}
