using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Xml.Linq;

namespace RESTMock.Core
{
    public class OperationConfig : IFluentOperationConfig
    {
        private HttpMethod httpMethod;
        
        private string completeRoute = "/";

        private string contentType;

        private int expectedInvoications = 0;

        private int actualInvocations = 0;

        private HttpStatusCode httpStatus = HttpStatusCode.OK;

        private IDictionary<string, object> expectedRequestHeaders;

        private IDictionary<string, string> responseHeaders;

        private Func<System.IO.Stream, OperationResponse<System.IO.Stream>> rawBodyProcessingCallback;

        private Func<string, OperationResponse<string>> stringBodyProcessingCallback;

        private Func<dynamic, OperationResponse<dynamic>> dynamicBodyProcessingCallback;

        private Func<Object, OperationResponse<Object>> typedBodyProcessingCallback;

        //Type TReq, TResp;

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

        public IFluentOperationConfig BodyProcessor(Func<System.IO.Stream, OperationResponse<System.IO.Stream>> handler)
        {
            if (rawBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }

            rawBodyProcessingCallback = handler;

            return this;
        }

        public IFluentOperationConfig BodyProcessor(Func<string, OperationResponse<string>> handler)
        {
            if (stringBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }
            
            stringBodyProcessingCallback = handler;

            return this;
        }

        //public IFluentOperationConfig BodyProcessor<TRequest,TResponse>(Func<TRequest, OperationResponse<TResponse>> handler)
        //{
        //    typedBodyProcessingCallback = (Func<object, OperationResponse<object>>)handler;
        //}

        public IFluentOperationConfig BodyProcessor(Func<dynamic, OperationResponse<dynamic>> handler)
        {
            if (dynamicBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }

            dynamicBodyProcessingCallback = handler;

            return this;
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

        public IFluentOperationConfig ResponseStatus(HttpStatusCode httpStatus) 
        {
            this.httpStatus = httpStatus;

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

            if (rawBodyProcessingCallback == null 
                && dynamicBodyProcessingCallback == null
                && stringBodyProcessingCallback == null)
            {
                throw new InvalidOperationException($"No body processing handler defined for operation {ToString()}");
            }

            IncreaseInvocations();

            // Typed body processor takes precedence over string based            
            if (dynamicBodyProcessingCallback != null)
            {
                var requestObject = DeserializeDynamicRequestObject(args.Context.Request);
                var operationResponse = dynamicBodyProcessingCallback((dynamic)requestObject);

                SendResponse(args.Context.Response, operationResponse);
            } 
            else if (rawBodyProcessingCallback != null)
            {
                var operationResponse = rawBodyProcessingCallback(args.Context.Request.InputStream);
                
                SendResponse(args.Context.Response, operationResponse);
            }
            else if (stringBodyProcessingCallback != null)
            {
                string bodyContents = ReadBodyAsString(args.Context.Request.InputStream, args.Context.Request.ContentEncoding);
                var operationResponse = stringBodyProcessingCallback(bodyContents);
                
                SendResponse(args.Context.Response, operationResponse);
            }

        }

        private dynamic DeserializeDynamicRequestObject(HttpListenerRequest request)
        {
            string requestContentType = request.ContentType;

            if (string.IsNullOrEmpty(requestContentType))
            {
                requestContentType = "application/json";
            }

            if (requestContentType.ToLowerInvariant().Contains("json"))
            {
                using (var reqReader = new StreamReader(request.InputStream))
                {
                    // Deserialize from Json
                    dynamic jDynamic = JsonConvert.DeserializeObject(reqReader.ReadToEnd());

                    return jDynamic;
                }
            }

            if (requestContentType.ToLowerInvariant().Contains("xml"))
            {
                // Deserialize from Json                
                dynamic xDynamic = DynamicXml.Load(request.InputStream);

                return xDynamic;
            }

            return null;
        }

        private string ReadBodyAsString(System.IO.Stream bodyStream, System.Text.Encoding encoding=null)
        {
            string body = null;
            using (var sr = new StreamReader(bodyStream, encoding == null ? 
                Encoding.UTF8 : encoding))
            {
                body = sr.ReadToEnd();
            }

            return body;
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

            httpResponse.StatusCode = (int)this.httpStatus;

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

                return resultHeaders.Count() == expectedRequestHeaders.Count(); // TODO: add verification on value too
            }
            else
                return true;
        }
    }
}
