using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ValidateModelStateAndResponseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ActionFilterUtil.ApiValidationError(context);
            base.OnActionExecuting(context);
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            ActionFilterUtil.RednerApiResponse(context);
            base.OnResultExecuting(context);
        }
    }
}
