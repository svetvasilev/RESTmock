using System;
using System.Collections.Generic;
using System.Net;

namespace RESTMock.Core
{
    public interface IFluentOperationConfig
    {
        IFluentOperationConfig QueryParam(string name, string value);

        IFluentOperationConfig Path(string pathSegment);

        IFluentOperationConfig RequestHeader(string name, string value);

        IFluentOperationConfig RequestHeaders(IDictionary<string, object> headers);

        IFluentOperationConfig ResponseHeaders(IDictionary<string,object> headers);

        IFluentOperationConfig ResponseStatus(HttpStatusCode httpStatus);

        IFluentOperationConfig ResponseBody(Func<OperationResponse<dynamic>> response);

        IFluentOperationConfig Accepts(string mimeType);

        IFluentOperationConfig ContentType(string contentType);

        IFluentOperationConfig Authorization(string authorization);

        IFluentOperationConfig BodyProcessor(Func<System.IO.Stream, OperationResponse<System.IO.Stream>> handler);

        IFluentOperationConfig BodyProcessor(Func<string, OperationResponse<string>> handler);

        // IFluentOperationConfig BodyProcessor<TRequest,TResponse>(Func<TRequest, OperationResponse<TResponse>> handler);

        IFluentOperationConfig BodyProcessor(Func<dynamic, OperationResponse<dynamic>> handler);


        void Verify();

    }
}