using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RESTMock.Core
{
    public class ServiceMock
    {
        private HttpListener httpListener;

        private event EventHandler<HttpContextArgs> RequestReceived;

        private ConcurrentDictionary<string, IFluentOperationUnknown> expectations;

        private Task mockRunner = null;

        private CancellationTokenSource tokenSource;

        public ServiceMock(string baseUri)
        {
            InitListener(baseUri);

            expectations = new ConcurrentDictionary<string, IFluentOperationUnknown>();
        }

        private void InitListener(params string[] prefixes)
        {
            httpListener = new HttpListener();

            foreach (string prefix in prefixes)
            {
                if (!prefix.EndsWith("/"))
                {
                    throw new NotSupportedException("The supplied base URI must end with '/'!");
                }

                httpListener.Prefixes.Add(prefix);
            }            
        }

        private async void Run()
        {
            while(httpListener.IsListening)
            {
                try
                {
                    var httpContext = await httpListener.GetContextAsync();

                    if (httpContext != null)
                    {
                        var receivedRequest = new HttpContextArgs()
                        {
                            Context = httpContext
                        };

                        OnRequestReceived(receivedRequest);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                }
                
            }
        }

        protected void OnRequestReceived(HttpContextArgs args)
        {
            //if (RequestReceived != null)
            //{
            //    RequestReceived(this, args);
            //}
            string operationKey = $"{args.Context.Request.HttpMethod}:{args.Context.Request.Url.PathAndQuery}"; // Same here - whether path only, or path and query, is more suitable
            IFluentOperationUnknown expectedOperation;
            
            // This will work well for concretely defined paths, but what about such with placeholders for parameters???
            while(!expectations.TryGetValue(operationKey, out expectedOperation)) { // Have to add some code here
            }

            if (expectedOperation == null)
            {
                throw new InvalidOperationException($"No expectation set up for operation {operationKey} !");
            }

            // TODO: Have to figure out a nicer way to do this
            var expectedOperationConfig = expectedOperation as IOperationRequestReceivedHandler;

            expectedOperationConfig.RequestReceived(this, args);
        }

        public async void Start()
        {
            await Task.Run(() => httpListener.Start());

            // Consider to build a concurrent dictionary for all the operations
            // before starting listening for connections
            // this way it will be possible to detect duplicates
            // as well as to keep track of number of invocations of services
            // in order to perform verifications after execution
            tokenSource = new CancellationTokenSource();            

            mockRunner = Task.Run(() => Run(), tokenSource.Token);
        }

        public async Task Stop()
        {
            tokenSource.Cancel();

            await Task.WhenAll(Task.Run(() => httpListener.Stop()), mockRunner);
            
            // await Task.Delay(250); // Adding tiny delay to allow for the cleanup to take place
        }

        /// <summary>
        /// Sets up a GET service mock with string response
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<string, string> SetupGet(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<string, string>(HttpMethod.Get, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a GET service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<TReq,TResp> SetupGet<TReq, TResp>(string path, int expectedInvoicationsCount=1)
        {
            return SetupOperation<TReq, TResp>(HttpMethod.Get, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a POST service mock with string request and response bodies
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<string, string> SetupPost(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<string, string>(HttpMethod.Post, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a POST service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<TReq, TResp> SetupPost<TReq, TResp>(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<TReq, TResp>(HttpMethod.Post, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a PUT service mock with string request and response bodies
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<string, string> SetupPut(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<string, string>(HttpMethod.Put, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a PUT service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<TReq, TResp> SetupPut<TReq, TResp>(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<TReq, TResp>(HttpMethod.Put, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a OPTIONS service mock with string request and response bodyes
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<string, string> SetupOptions(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<string, string>(HttpMethod.Options, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a OPTIONS service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<TReq, TResp> SetupOptions<TReq, TResp>(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<TReq, TResp>(HttpMethod.Options, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a DELETE service mock with string request and response bodies
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<string, string> SetupDelete(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<string, string>(HttpMethod.Delete, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a DELETE service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig<TReq, TResp> SetupDelete<TReq, TResp>(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation<TReq, TResp>(HttpMethod.Delete, path, expectedInvoicationsCount);
        }

        private OperationConfig<TReq, TResp> SetupOperation<TReq, TResp>(HttpMethod httpMethod, string path, int expectedInvoicationsCount)
        {
            var operationConfig = new OperationConfig<TReq, TResp>(HttpMethod.Get, expectedInvoicationsCount); 
            
            operationConfig.Path(path);
            operationConfig.PathChanged += OperationConfig_PathChanged;

            RequestReceived += operationConfig.RequestReceived;

            expectations.TryAdd(operationConfig.ToString(), (IFluentOperationUnknown)operationConfig);

            return operationConfig;
        }

        private void OperationConfig_PathChanged(object sender, PathChangedArgs e)
        {
            IFluentOperationUnknown operation = null;
            expectations.TryRemove(e.OldPath, out operation);

            if (operation != null)
            {
                expectations.TryAdd(e.NewPath, operation);
            }
        }
    }
}
