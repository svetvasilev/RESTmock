using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RESTMock.Core
{
    public class OperationConfig : IFluentOperationConfig
    {
        private string httpMethod;
        
        private string completeRoute = "/";

        private string contentType;

        private IDictionary<string, object> expectedRequestHeaders;

        private IDictionary<string, string> responseHeaders;

        private Func<System.IO.Stream, OperationResponse> rawBodyProcessingCallback;

        public OperationConfig(string httpMethod)
        {
            this.httpMethod = httpMethod;

            expectedRequestHeaders = new Dictionary<string, object>();

            responseHeaders = new Dictionary<string, string>();
        }

        public IFluentOperationConfig Accepts(string mimeType)
        {
            expectedRequestHeaders.Add("Accepts", mimeType);

            return this;
        }

        public IFluentOperationConfig Authorization(string authorization)
        {
            expectedRequestHeaders.Add("Authorization", authorization);

            return this;
        }

        public IFluentOperationConfig ContentType(string contentType)
        {
            responseHeaders.Add("ContentType", contentType);

            return this;
        }

        public IFluentOperationConfig BodyProcessor(Func<System.IO.Stream, OperationResponse> handler)
        {
            if (rawBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }

            rawBodyProcessingCallback = handler;

            return this;
        }

        internal void RequestReceivedHandler(object sender, HttpContextArgs args)
        {
            //if (args.Context.Request.Url.PathAndQuery == completeRoute 
            //    && args.Context.Request.HttpMethod == httpMethod)
            //{
                // TODO: perform the other checks on request headers
                // When all tests are ok, invoke first the RequestContents validator, and then finally CreateResponse callback

                if (!CheckHeaders(args.Context.Request))
                {
                    throw new InvalidOperationException($"Request headers do not match for operation {httpMethod} {completeRoute}");
                }

                if (rawBodyProcessingCallback == null)
                {
                    throw new InvalidOperationException($"No body processing handler defined for operation {ToString()}");
                }

                var operationResponse = rawBodyProcessingCallback(args.Context.Request.InputStream);
                SendResponse(args.Context.Response, operationResponse);
            //}
        }

        private void SendResponse(HttpListenerResponse httpResponse, OperationResponse operationResponse)
        {
            // Adding global headers
            foreach (var header in responseHeaders)
            {
                httpResponse.AddHeader(header.Key, header.Value);
            }
            //Adding operation headers
            foreach (var header in operationResponse.ResponseHeaders)
            {
                httpResponse.AddHeader(header.Key, header.Value);
            }

            // rewinding the response body stream just in case
            var operationResponseStream = operationResponse.RawBody;
            operationResponseStream.Position = 0;

            byte[] responseBodyArray = new byte[operationResponseStream.Length];
            operationResponseStream.Read(responseBodyArray, 0, responseBodyArray.Length);

            httpResponse.OutputStream.Write(responseBodyArray, 0, responseBodyArray.Length);
            httpResponse.OutputStream.Flush();
            httpResponse.OutputStream.Close();
            
        }

        public IFluentOperationConfig Path(string pathSegment)
        {
            completeRoute += pathSegment;

            return this;
        }

        public IFluentOperationConfig QueryParam(string name, string value)
        {
            if (!completeRoute.Contains("?") && !completeRoute.EndsWith("?"))
            {
                completeRoute += $"?{name}={value}";
            }
            else
                completeRoute += $"&{name}={value}";

            return this;
        }

        public IFluentOperationConfig RequestHeader(string name, string value)
        {
            expectedRequestHeaders.Add(name, value);

            return this;
        }

        public IFluentOperationConfig RequestHeaders(IDictionary<string, object> headers)
        {
            expectedRequestHeaders.Concat(headers);

            return this;
        }

        public IFluentOperationConfig ResponseHeaders(IDictionary<string, object> headers)
        {
            expectedRequestHeaders.Concat(headers);

            return this;
        }

        public override string ToString()
        {
            return $"{httpMethod}:{completeRoute}"; // Have to see whether the completeRoute or just the path is more suitable
        }

        private bool CheckHeaders(HttpListenerRequest req)
        {
            if (req.Headers.HasKeys())
            {
                if (expectedRequestHeaders.Count() == 0)
                {
                    throw new InvalidOperationException("Attempting to validate request headers against empty headers expectation.");
                }

                var requestHeaders = req.Headers.ToDictionary();

                var resultHeaders = requestHeaders.Intersect(expectedRequestHeaders);

                return resultHeaders.Count() == expectedRequestHeaders.Count(); // TODO: add valuation on value too
            }
            else
                return true;
        }
    }
}
