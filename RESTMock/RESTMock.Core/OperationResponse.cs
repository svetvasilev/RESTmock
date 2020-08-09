using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RESTMock.Core
{
    public class OperationResponse
    {
        public OperationResponse()
        {
            ResponseHeaders = new Dictionary<string, string>(3);
        }
        public HttpStatusCode StatusCode { get; set; }

        public IDictionary<string, string> ResponseHeaders { get; set; }

        public System.IO.Stream RawBody { get; set; }
    }
}
