using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace RESTMock.Core
{
    public class OperationResponse<T>
    {
        internal System.IO.Stream RawBody { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public IDictionary<string, string> ResponseHeaders { get; set; }

        public T Body { get; set; }

        public OperationResponse()
        {
            ResponseHeaders = new Dictionary<string, string>(3);
        }


    }
}
