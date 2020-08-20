using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RESTMock.Core
{
    public class OperationConfig : IFluentOperationConfig
    {
        private HttpMethod httpMethod;
        
        private string completeRoute = "/";

        private string contentType;

        private int expectedInvoications = 0;

        private int actualInvocations = 0;

        private IDictionary<string, object> expectedRequestHeaders;

        private IDictionary<string, string> responseHeaders;

        private Func<System.IO.Stream, OperationResponse<string>> rawBodyProcessingCallback;

        public OperationConfig(HttpMethod httpMethod)
        {
            this.httpMethod = httpMethod;

            expectedRequestHeaders = new Dictionary<string, object>();

            responseHeaders = new Dictionary<string, string>();
        }

        public OperationConfig(HttpMethod httpMethod, int expectedInvocationCount) : this(httpMethod)
        {
            expectedInvoications = expectedInvocationCount;
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

        public IFluentOperationConfig BodyProcessor(Func<System.IO.Stream, OperationResponse<string>> handler)
        {
            if (rawBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }

            rawBodyProcessingCallback = handler;

            return this;
        }

        public IFluentOperationConfig BodyProcessor<T>(Func<System.IO.Stream, OperationResponse<T>> handler)
        {
            throw new NotImplementedException();
        }        

        //public void WriteBody<T>(T objectContents)
        //{

        //}

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

        public void Verify()
        {
            if (expectedInvoications != actualInvocations)
            {
                throw new ApplicationException($"Number of actual invocations ({actualInvocations}) differs from expected invocations for service {ToString()}");
            }
        }

        public override string ToString()
        {
            return $"{httpMethod.Method.ToUpper()}:{completeRoute}"; // Have to see whether the completeRoute or just the path is more suitable
        }

        internal void RequestReceivedHandler(object sender, HttpContextArgs args)
        {
            if (!CheckHeaders(args.Context.Request))
            {
                throw new InvalidOperationException($"Request headers do not match for operation {httpMethod} {completeRoute}");
            }

            if (rawBodyProcessingCallback == null)
            {
                throw new InvalidOperationException($"No body processing handler defined for operation {ToString()}");
            }

            IncreaseInvocations();

            var operationResponse = rawBodyProcessingCallback(args.Context.Request.InputStream);
            SendResponse(args.Context.Response, operationResponse);

        }

        internal void IncreaseInvocations()
        {
            actualInvocations++;
        }

        private void SendResponse<T>(HttpListenerResponse httpResponse, OperationResponse<T> operationResponse)
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

            WriteBody(operationResponse.Body, httpResponse);

        }

        private void WriteBody<T>(T contents, HttpListenerResponse httpResponse)
        {
            if (typeof(T) == typeof(string))
            {
                using (var memStream = new MemoryStream(256))
                {
                    using (var responseWriter = new StreamWriter(memStream))
                    {
                        responseWriter.Write(contents);
                        responseWriter.Flush();
                        // Rewinding the stream so that it is readable at next invocation
                        memStream.Seek(0, SeekOrigin.Begin);

                        httpResponse.OutputStream.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        httpResponse.OutputStream.Flush();
                        httpResponse.OutputStream.Close();
                    }

                }
            }
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
