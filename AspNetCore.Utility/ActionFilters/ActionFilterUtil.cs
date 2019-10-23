using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using EntityFramework;
using DotNetCore;
namespace AspNetCore
{
    public class ActionFilterUtil
    {
        #region Api
        public static void RednerApiResponse(ResultExecutingContext context)
        {
            if (ApiValidationError(context))
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
        }

        public static bool ApiValidationError(FilterContext context)
        {
            Type controllerType = GetFilterContextControllerType(context);
            bool isValid = false;

            if (IsApiController(controllerType))
            {
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
                else
                {
                    isValid = true;
                }
            }
            return isValid;
        }

        public static void HandleApiExceptionActionFilter(ExceptionContext context)
        {
            Type controllerType = GetFilterContextControllerType(context);

            if (IsApiController(controllerType))
            {
                var response = new SingleResponse<string>();
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
                    //TODO.Log exception
                    Console.WriteLine(ex.GetStackStrace());
                    response.ServerErrorMessages = new List<string> { "Error" };
                }
                context.Result = response.ToHttpResponse();
            }
        }


        public static bool IsApiController(Type controllerType)
        {
            bool isApiController = false;
            var controllerBase = typeof(ApiControllerBase);
            var controller = typeof(Controller);

            // Api's must implement ApiControllerBase
            if (controllerType.IsSubclassOf(controllerBase) && !controllerType.IsSubclassOf(controller))
            {
                isApiController = true;
            }
            return isApiController;
        }

        #endregion

        #region Mvc

        public static void HandleMvcExceptionActionFilter(ExceptionContext context)
        {
            Type controllerType = GetFilterContextControllerType(context);
            // Handle normal controller exception.TODO.impl it.
            if (IsMvcController(controllerType))
            {
            }
        }

        public static bool IsMvcController(Type controllerType)
        {
            bool isMvcController = false;
            var controllerBase = typeof(ApiControllerBase);
            var controller = typeof(Controller);

            if (controllerType.IsSubclassOf(controllerBase) && controllerType.IsSubclassOf(controller))
            {
                isMvcController = true;
            }
            return isMvcController;
        }

        #endregion

        #region FilterContext

        static Type GetFilterContextControllerType(FilterContext context)
        {
            var actionDescriptor = (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor;
            Type controllerType = actionDescriptor.ControllerTypeInfo;
            return controllerType;
        }

        #endregion
    }
}
