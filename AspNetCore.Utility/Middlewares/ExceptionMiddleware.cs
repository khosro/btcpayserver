using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore
{
    //Comment#1 TODO.This class not tested instead we use ExceptionActionFilter
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new SingleResponse<string>();

            try
            {
                exception.HandleException();
            }
            catch (UniqueConstraintException ex)
            {
                context.Response.StatusCode = (int)ResponseBase.DefaultHttpStatusCodeForErrorMessages;
                response.ErrorMessages = new List<string> { ex.Message };
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)ResponseBase.DefaultHttpStatusCodeForServerErrorMessages;
                response.ServerErrorMessages = new List<string> { "Error" };
            }

            return context.Response.WriteAsync(response.ToString());
        }
    }
}
