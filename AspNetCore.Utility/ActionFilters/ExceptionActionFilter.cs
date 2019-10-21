using System;
using System.Collections.Generic;
using System.Text;
using EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore
{
    public class ExceptionActionFilter : ExceptionFilterAttribute
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public ExceptionActionFilter()
        {
        }

        #region Overrides of ExceptionFilterAttribute

        public override void OnException(ExceptionContext context)
        {
            var actionDescriptor = (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor;
            Type controllerType = actionDescriptor.ControllerTypeInfo;

            var controllerBase = typeof(ApiControllerBase);
            var controller = typeof(Controller);
            var response = new SingleResponse<string>();

            // Api's implements ApiControllerBase but not Controller
            if (controllerType.IsSubclassOf(controllerBase) && !controllerType.IsSubclassOf(controller))
            {
                try
                {
                    context.Exception.HandleException();
                }
                catch (UniqueConstraintException ex)
                {
                    response.ErrorMessages = new List<string> { ex.Message };
                }
                catch (Exception ex)
                {
                    response.ServerErrorMessages = new List<string> { "Error" };
                }

                context.Result = response.ToHttpResponse();
            }

            // Pages implements ControllerBase and Controller
            if (controllerType.IsSubclassOf(controllerBase) && controllerType.IsSubclassOf(controller))
            {
                // Handle normal controller exception
            }

            base.OnException(context);
        }

        #endregion
    }
}
