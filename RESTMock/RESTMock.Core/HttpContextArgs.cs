using System;
using System.Net;

namespace RESTMock.Core
{
    public class HttpContextArgs : EventArgs
    {
        public HttpListenerContext Context { get; set; }
    }
}