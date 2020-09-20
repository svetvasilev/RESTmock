using System;
using System.Collections.Generic;
using System.Net;

namespace RESTMock.Core
{
    /// <summary>
    /// Defines the fluent syntax operations of a mocked service
    /// </summary>
    /// <typeparam name="TReq">The request type</typeparam>
    /// <typeparam name="TResp">The response type</typeparam>
    public interface IFluentOperationConfig<TReq, TResp> : IFluentOperationUnknown
    {
        IFluentOperationConfig<TReq, TResp> QueryParam(string name, string value);

        IFluentOperationConfig<TReq, TResp> Path(string pathSegment);

        IFluentOperationConfig<TReq, TResp> RequestHeader(string name, string value);

        IFluentOperationConfig<TReq, TResp> RequestHeaders(IDictionary<string, object> headers);

        IFluentOperationConfig<TReq, TResp> ResponseHeaders(IDictionary<string,object> headers);

        IFluentOperationConfig<TReq, TResp> ResponseStatus(HttpStatusCode httpStatus);

        IFluentOperationConfig<TReq, TResp> ResponseBody (Func<TResp> responseBody);

        IFluentOperationConfig<TReq, TResp> Accepts(string mimeType);

        IFluentOperationConfig<TReq, TResp> ContentType(string contentType);

        IFluentOperationConfig<TReq, TResp> Authorization(string authorization);

        IFluentOperationConfig<TReq, TResp> BodyProcessor(Func<TReq, TResp> handler);

        void Verify();

    }
}