using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RESTMock.Core
{
    /// <summary>
    /// Concrete implementation of an expected operation configuration
    /// </summary>
    /// <typeparam name="TReq">The expected request type</typeparam>
    /// <typeparam name="TResp">The expected response type</typeparam>
    public class OperationConfig<TReq, TResp> : IFluentOperationConfig<TReq, TResp>, IOperationRequestReceivedHandler
    {
        private HttpMethod httpMethod;
        
        private string completeRoute = "/";

        private string contentType;

        private int expectedInvoications = 0;

        private int actualInvocations = 0;

        private HttpStatusCode httpStatus = HttpStatusCode.OK;

        private IDictionary<string, object> expectedRequestHeaders;

        private IDictionary<string, string> responseHeaders;

        private Func<TReq, TResp> rawBodyProcessingCallback;

        //private Func<string, OperationResponse<string>> stringBodyProcessingCallback;

        //private Func<dynamic, OperationResponse<dynamic>> dynamicBodyProcessingCallback;

        //private Func<Object, OperationResponse<Object>> typedBodyProcessingCallback;

        private Func<OperationResponse<dynamic>> dynamicResponseBodyHandler;

        private Func<TResp> basicResponseBodyHandler;

        internal event EventHandler<PathChangedArgs> PathChanged;

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

        public IFluentOperationConfig<TReq, TResp> Accepts(string mimeType)
        {
            expectedRequestHeaders.Add("Accepts", mimeType);

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> Authorization(string authorization)
        {
            expectedRequestHeaders.Add("Authorization", authorization);

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> ContentType(string contentType)
        {
            this.contentType = contentType;
            responseHeaders.Add("Content-Type", contentType);

            return this;
        }        

        public IFluentOperationConfig<TReq, TResp> ResponseBody(Func<TResp> responseBody)
        {
            if (basicResponseBodyHandler != null)
            {
                throw new InvalidOperationException($"There has already been registered response body handler for operation: {ToString()}");
            }

            basicResponseBodyHandler = responseBody;

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> BodyProcessor(Func<TReq, TResp> handler)
        {
            if (rawBodyProcessingCallback != null)
            {
                throw new InvalidOperationException($"There has already been registered body processor for operation: {ToString()}");
            }

            rawBodyProcessingCallback = handler;

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> Path(string pathSegment)
        {
            string oldPath = ToString();
            
            completeRoute += pathSegment;

            OnPathChanged(oldPath, ToString());

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> QueryParam(string name, string value)
        {
            string oldPath = ToString();

            if (!completeRoute.Contains("?") && !completeRoute.EndsWith("?"))
            {
                completeRoute += $"?{name}={value}";
            }
            else
                completeRoute += $"&{name}={value}";

            OnPathChanged(oldPath, ToString());

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> RequestHeader(string name, string value)
        {
            expectedRequestHeaders.Add(name, value);

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> RequestHeaders(IDictionary<string, object> headers)
        {
            expectedRequestHeaders.Concat(headers);

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> ResponseHeaders(IDictionary<string, object> headers)
        {
            expectedRequestHeaders.Concat(headers);

            return this;
        }

        public IFluentOperationConfig<TReq, TResp> ResponseStatus(HttpStatusCode httpStatus) 
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

        public string Operation { 
            get {
                return ToString();
            } 
        }

        public override string ToString()
        {
            return $"{httpMethod.Method.ToUpper()}:{completeRoute}"; // Have to see whether the completeRoute or just the path is more suitable
        }

        public void RequestReceived(object sender, HttpContextArgs args)
        {
            if (!CheckHeaders(args.Context.Request))
            {
                throw new InvalidOperationException($"Request headers do not match for operation {httpMethod} {completeRoute}");
            }

            if (rawBodyProcessingCallback == null                 
                && basicResponseBodyHandler == null)
            {
                throw new InvalidOperationException($"No body processing handler defined for operation {ToString()}");
            }

            IncreaseInvocations();
                        
            if (rawBodyProcessingCallback != null)
            {
                var requestObject = DeserializeBody(args.Context.Request);
                var respoObject = rawBodyProcessingCallback(requestObject);

                var operationResponse = new OperationResponse<TResp>()
                {
                    Body = respoObject
                };
                
                SendResponse(args.Context.Response, operationResponse);
            }

            if (basicResponseBodyHandler != null)
            {
                // var requestObject = DeserializeBody(args.Context.Request);
                var respoObject = basicResponseBodyHandler();

                var operationResponse = new OperationResponse<TResp>()
                {
                    Body = respoObject
                };

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

        private TReq DeserializeBody(HttpListenerRequest request)
        {
            TReq requestObject = default(TReq);

            string requestContentType = request.ContentType;

            if (string.IsNullOrEmpty(requestContentType))
            {
                requestContentType = "text/text";
            }

            switch (request.ContentType)
            {
                case string json when json.ToLowerInvariant().Contains("json") && typeof(TReq) != typeof(string):
                    requestObject = DeserializeJsonBody(request.InputStream);
                    break;
                case string xml when xml.ToLowerInvariant().Contains("xml") && typeof(TReq) != typeof(string):
                    requestObject = DeserializeXmlBody(request.InputStream);
                    break;
                default:                    
                    requestObject = DeserializeRawBody(request.InputStream);
                    break;
            }

            return requestObject;
        }

        private static TReq DeserializeJsonBody(Stream bodyStream)
        {
            using (var reqReader = new StreamReader(bodyStream))
            {
                // Deserialize from Json
                var requestObject = JsonConvert.DeserializeObject<TReq>(reqReader.ReadToEnd());

                return requestObject;
            }
        }

        private static TReq DeserializeXmlBody(Stream bodyStream)
        {
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TReq));

            var requestObject = (TReq)xmlSerializer.Deserialize(bodyStream);

            return requestObject;
        }

        private static TReq DeserializeRawBody(Stream bodyStream)
        {
            using (var reqReader = new StreamReader(bodyStream))
            {
                // Deserialize from Json
                string requestObject = reqReader.ReadToEnd();

                return (TReq)Convert.ChangeType(requestObject, typeof(TReq));
            }
        }

        internal void IncreaseInvocations()
        {
            actualInvocations++;
        }

        private void SendResponse(HttpListenerResponse httpResponse, OperationResponse<TResp> operationResponse)
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

            // WriteBody(operationResponse.Body, httpResponse);
            WriteResponseBody(operationResponse.Body, httpResponse);

        }

        private void WriteResponseBody(TResp contents, HttpListenerResponse httpResponse)
        {
            try
            {
                switch (httpResponse.ContentType)
                {
                    case string json when json.ToLowerInvariant().Contains("json") && typeof(TResp) != typeof(string):
                        string responseBody = JsonConvert.SerializeObject(contents);

                        WriteResponseToOutputStream(httpResponse, responseBody);
                        break;
                    case string xml when xml.ToLowerInvariant().Contains("xml") && typeof(TResp) != typeof(string):
                        var xmlSerializer = new XmlSerializer(typeof(TResp));

                        using (var sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, contents);

                            WriteResponseToOutputStream(httpResponse, sw.ToString());
                        }
                        break;
                    default:
                        using (var sw = new StringWriter())
                        {
                            sw.Write(contents);

                            WriteResponseToOutputStream(httpResponse, sw.ToString());
                        }
                        break;
                }

                
            }
            finally
            {
                httpResponse.Close();
            }
        }

        private void WriteBody<T>(T contents, HttpListenerResponse httpResponse)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    WriteResponseToOutputStream(httpResponse, contents.ToString());
                }
                else if (typeof(T) == typeof(object))
                {
                    // Handling dynamic objects
                    if (contentType.ToLowerInvariant().Contains("json"))
                    {
                        string responseBody = JsonConvert.SerializeObject(contents);

                        WriteResponseToOutputStream(httpResponse, responseBody);
                    }
                }
            }
            finally
            {
                httpResponse.Close();
            }
        }

        private static void WriteResponseToOutputStream(HttpListenerResponse httpResponse, string responseBody)
        {
            using (var memStream = new MemoryStream(256))
            {
                using (var responseWriter = new StreamWriter(memStream))
                {
                    responseWriter.Write(responseBody);
                    responseWriter.Flush();
                    // Rewinding the stream so that it is readable at next invocation
                    memStream.Seek(0, SeekOrigin.Begin);

                    httpResponse.OutputStream.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    httpResponse.OutputStream.Flush();
                    httpResponse.OutputStream.Close();
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

        protected void OnPathChanged(string oldPath, string newPath)
        {
            PathChanged?.Invoke(this, new PathChangedArgs()
            {
                OldPath = oldPath,
                NewPath = newPath
            });
        }
    }
}
