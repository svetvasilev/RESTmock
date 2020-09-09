using System;
using System.Collections.Generic;
using System.Net;

namespace RESTMock.Core
{
    public interface IFluentOperationConfig<TReq, TResp> : IFluentOperationUnknown
    {
        IFluentOperationConfig<TReq, TResp> QueryParam(string name, string value);

        IFluentOperationConfig<TReq, TResp> Path(string pathSegment);

        IFluentOperationConfig<TReq, TResp> RequestHeader(string name, string value);

        IFluentOperationConfig<TReq, TResp> RequestHeaders(IDictionary<string, object> headers);

        IFluentOperationConfig<TReq, TResp> ResponseHeaders(IDictionary<string,object> headers);

        IFluentOperationConfig<TReq, TResp> ResponseStatus(HttpStatusCode httpStatus);

        IFluentOperationConfig<TReq, TResp> ResponseBody(Func<OperationResponse<dynamic>> response);

        IFluentOperationConfig<TReq, TResp> ResponseBody (Func<TResp> responseBody);

        IFluentOperationConfig<TReq, TResp> Accepts(string mimeType);

        IFluentOperationConfig<TReq, TResp> ContentType(string contentType);

        IFluentOperationConfig<TReq, TResp> Authorization(string authorization);

        IFluentOperationConfig<TReq, TResp> BodyProcessor(Func<TReq, TResp> handler);

        void Verify();

    }

    public interface IFluentOperationUnknown
    {
        string Operation { get; }
    }

    public interface IOperationRequestProcessor
    {
        void ProcessRequest(object sender, HttpContextArgs args);
    }
}