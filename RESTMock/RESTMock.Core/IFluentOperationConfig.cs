using System;
using System.Collections.Generic;

namespace RESTMock.Core
{
    public interface IFluentOperationConfig
    {
        IFluentOperationConfig QueryParam(string name, string value);

        IFluentOperationConfig Path(string pathSegment);

        IFluentOperationConfig RequestHeader(string name, string value);

        IFluentOperationConfig RequestHeaders(IDictionary<string, object> headers);

        IFluentOperationConfig ResponseHeaders(IDictionary<string,object> headers);

        IFluentOperationConfig Accepts(string mimeType);

        IFluentOperationConfig ContentType(string contentType);

        IFluentOperationConfig Authorization(string authorization);

        IFluentOperationConfig BodyProcessor(Func<System.IO.Stream, OperationResponse> handler);

    }
}