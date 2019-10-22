using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
namespace AspNetCore
{
    #region Interfaces
    public interface IResponse
    {
        List<string> Messages { get; set; }

        bool HasError { get; }

        IList<string> ErrorMessages { get; set; }

        IList<string> ServerErrorMessages { get; set; }
    }

    public interface ISingleResponse<TModel> : IResponse
    {
        TModel Model { get; set; }
    }

    public interface IListResponse<TModel> : IResponse
    {
        IEnumerable<TModel> Model { get; set; }
    }

    public interface IPagedResponse<TModel> : IListResponse<TModel>
    {
        long ItemsCount { get; set; }

        double PageCount { get; }

        long PageSize { get; set; }

        long PageNumber { get; set; }
    }
    #endregion Interfaces

    #region Abstract classes

    public abstract class ResponseBase : IResponse
    {
        public const HttpStatusCode DefaultHttpStatusCodeForErrorMessages = HttpStatusCode.PreconditionFailed;
        public const HttpStatusCode DefaultHttpStatusCodeForServerErrorMessages = HttpStatusCode.InternalServerError;
        public const HttpStatusCode DefaultHttpStatusCode = 0;

        IList<string> errorMessages;
        IList<string> serverErrorMessages;
        bool _hasError;

        public ResponseBase()
        {
            ErrorMessages = new List<string>();
            ServerErrorMessages = new List<string>();
            Messages = new List<string>();
        }

        public List<string> Messages { get; set; }

        public bool HasError
        {
            get { return _hasError || ErrorMessages.Any() || ServerErrorMessages.Any(); }
        }

        public IList<string> ErrorMessages
        {
            get
            {
                errorMessages = errorMessages.Where(t => !string.IsNullOrEmpty(t)).ToList();
                return errorMessages;
            }
            set { errorMessages = value; }
        }
        public IList<string> ServerErrorMessages
        {
            get
            {
                serverErrorMessages = serverErrorMessages.Where(t => !string.IsNullOrEmpty(t)).ToList();
                return serverErrorMessages;
            }
            set { serverErrorMessages = value; }
        }
    }

    public abstract class SingleResponseBase<TModel> : ResponseBase, ISingleResponse<TModel>
    {
        public TModel Model { get; set; }
    }

    public abstract class ListResponseBase<TModel> : ResponseBase, IListResponse<TModel>
    {
        public IEnumerable<TModel> Model { get; set; }
    }

    public abstract class PagedResponseBase<TModel> : ListResponseBase<TModel>, IPagedResponse<TModel>
    {
        public long ItemsCount { get; set; }

        public abstract double PageCount { get; }

        public long PageSize { get; set; }

        public long PageNumber { get; set; }
    }
    #endregion Abstract classes

    #region Implementation

    public class Response : ResponseBase
    { }

    public class SingleResponse<TModel> : SingleResponseBase<TModel>
    { }

    public class ListResponse<TModel> : ListResponseBase<TModel>
    { }

    public class PagedResponse<TModel> : PagedResponseBase<TModel>
    {
        public override double PageCount => ItemsCount < PageSize ? 1 : (int)(((double)ItemsCount / PageSize) + 1);
    }
    #endregion Implementation

    public static class ResponseExtensions
    {
        #region Obsolete
        /* I think it does not need any more.The only difference is "HttpStatusCode.Created"
         * public static IActionResult ToHttpCreatedResponse<TModel>(this ISingleResponse<TModel> response)
         {
             var status = HttpStatusCode.Created;

             return new ObjectResult(response)
             {
                 StatusCode = (int)GetHttpStatusCode<TModel>(response, status)
             };
         }
         */
        #endregion

        public static IActionResult ToHttpResponse(this IResponse response)
        {
            return new ObjectResult(response)
            {
                StatusCode = (int)GetHttpStatusCode(response)
            };
        }
        public static IActionResult ToHttpResponse<TModel>(this ISingleResponse<TModel> response)
        {
            var status = HttpStatusCode.OK;

            return new ObjectResult(response)
            {
                StatusCode = (int)GetHttpStatusCode<TModel>(response, status)
            };
        }

        public static IActionResult ToHttpResponse<TModel>(this IListResponse<TModel> response)
        {
            var status = HttpStatusCode.OK;

            return new ObjectResult(response)
            {
                StatusCode = (int)GetHttpStatusCode<TModel>(response, status)
            };
        }

        static HttpStatusCode GetHttpStatusCode<TModel>(IResponse response, HttpStatusCode status)
        {
            ISingleResponse<TModel> singleResponse = null;
            IListResponse<TModel> listResponse = null;
            if (response is ISingleResponse<TModel>)
            {
                singleResponse = (ISingleResponse<TModel>)response;
            }
            else if (response is IListResponse<TModel>)

            {
                listResponse = (IListResponse<TModel>)response;
            }
            var errorStatusCode = GetHttpStatusCode(response);

            if (errorStatusCode == ResponseBase.DefaultHttpStatusCode &&
            ((singleResponse != null && singleResponse.Model == null) || (listResponse != null && listResponse.Model == null)))
                status = HttpStatusCode.NoContent;
            else if (errorStatusCode != ResponseBase.DefaultHttpStatusCode)
                status = errorStatusCode;

            return status;
        }

        static HttpStatusCode GetHttpStatusCode(IResponse response)
        {
            HttpStatusCode status = ResponseBase.DefaultHttpStatusCode;

            if (response.ServerErrorMessages.Any(t => !string.IsNullOrEmpty(t)))
                status = ResponseBase.DefaultHttpStatusCodeForServerErrorMessages;
            else if (response.ErrorMessages.Any(t => !string.IsNullOrEmpty(t)))
                status = ResponseBase.DefaultHttpStatusCodeForErrorMessages;

            return status;
        }
    }
}
