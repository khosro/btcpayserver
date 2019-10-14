using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc;
namespace BTCPayServer
{


    #region Interfaces
    public interface IResponse
    {
        string Message { get; set; }

        bool HasError { get; }

        string ErrorMessage { get; set; }

        string ServerErrorMessage { get; set; }
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
        bool _hasError;
        public string Message { get; set; }

        public bool HasError
        {
            get { return _hasError || !string.IsNullOrWhiteSpace(ErrorMessage) || !string.IsNullOrWhiteSpace(ServerErrorMessage); }
        }

        public string ErrorMessage { get; set; }
        public string ServerErrorMessage { get; set; }
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

    public static class ResponseExtensions
    {
        public static IActionResult ToHttpResponse(this IResponse response)
            => new ObjectResult(response)
            {
                StatusCode = (int)(response.HasError ? HttpStatusCode.InternalServerError : HttpStatusCode.OK)
            };

        public static IActionResult ToHttpResponse<TModel>(this ISingleResponse<TModel> response)
        {
            var status = HttpStatusCode.OK;

            if (!string.IsNullOrWhiteSpace(response.ServerErrorMessage))
                status = HttpStatusCode.InternalServerError;
            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                status = HttpStatusCode.PreconditionFailed;
            else if (response.Model == null)
                status = HttpStatusCode.NotFound;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }

        public static IActionResult ToHttpCreatedResponse<TModel>(this ISingleResponse<TModel> response)
        {
            var status = HttpStatusCode.Created;

            if (!string.IsNullOrWhiteSpace(response.ServerErrorMessage))
                status = HttpStatusCode.InternalServerError;
            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                status = HttpStatusCode.PreconditionFailed;
            else if (response.Model == null)
                status = HttpStatusCode.NotFound;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }

        public static IActionResult ToHttpResponse<TModel>(this IListResponse<TModel> response)
        {
            var status = HttpStatusCode.OK;

            if (!string.IsNullOrWhiteSpace(response.ServerErrorMessage))
                status = HttpStatusCode.InternalServerError;
            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                status = HttpStatusCode.PreconditionFailed;
            else if (response.Model == null)
                status = HttpStatusCode.NoContent;

            return new ObjectResult(response)
            {
                StatusCode = (int)status
            };
        }
    }

}
