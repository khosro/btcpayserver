using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ExceptionActionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            ActionFilterUtil.HandleApiExceptionActionFilter(context);

            ActionFilterUtil.HandleMvcExceptionActionFilter(context);

            base.OnException(context);
        }
    }
}
