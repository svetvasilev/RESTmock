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

        private ConcurrentDictionary<string, IFluentOperationConfig> expectations;

        public ServiceMock(string baseUri)
        {
            InitListener(baseUri);

            expectations = new ConcurrentDictionary<string, IFluentOperationConfig>();
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
                catch (HttpListenerException ex)
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
            IFluentOperationConfig expectedOperation;
            
            // This will work well for concretely defined paths, but what about such with placeholders for parameters???
            while(!expectations.TryGetValue(operationKey, out expectedOperation)) { // Have to add some code here
            }

            if (expectedOperation == null)
            {
                throw new InvalidOperationException($"No expectation set up for operation {operationKey} !");
            }

            // TODO: Have to figure out a nicer way to do this
            var expectedOperationConfig = expectedOperation as OperationConfig;

            expectedOperationConfig.RequestReceivedHandler(this, args);
        }

        public async void Start()
        {
            await Task.Run(() => httpListener.Start());

            // Consider to build a concurrent dictionary for all the operations
            // before starting listening for connections
            // this way it will be possible to detect duplicates
            // as well as to keep track of number of invocations of services
            // in order to perform verifications after execution

            Task.Run(() => Run());
        }

        public async Task Stop()
        {
            await Task.Run(() => httpListener.Stop());
            await Task.Delay(250); // Adding tiny delay to allow for the cleanup to take place
        }

        /// <summary>
        /// Sets up a GET service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig SetupGet(string path, int expectedInvoicationsCount=1)
        {
            return SetupOperation(HttpMethod.Get, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a POST service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig SetupPost(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation(HttpMethod.Post, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a PUT service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig SetupPut(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation(HttpMethod.Put, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a OPTIONS service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig SetupOptions(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation(HttpMethod.Options, path, expectedInvoicationsCount);
        }

        /// <summary>
        /// Sets up a DELETE service mock
        /// </summary>
        /// <param name="path">The path on which the service will response</param>
        /// <param name="expectedInvoicationsCount">Optional number of expected invocations. Default is 1.</param>
        /// <returns>An instance of a class implementing the <see cref="IFluentOperationConfig"/> interface</returns>
        public IFluentOperationConfig SetupDelete(string path, int expectedInvoicationsCount = 1)
        {
            return SetupOperation(HttpMethod.Delete, path, expectedInvoicationsCount);
        }

        private OperationConfig SetupOperation(HttpMethod httpMethod, string path, int expectedInvoicationsCount)
        {
            var operationConfig = new OperationConfig(HttpMethod.Get, expectedInvoicationsCount); 
            
            operationConfig.Path(path);
            operationConfig.PathChanged += OperationConfig_PathChanged;

            RequestReceived += operationConfig.RequestReceivedHandler;

            expectations.TryAdd(operationConfig.ToString(), operationConfig);

            return operationConfig;
        }

        private void OperationConfig_PathChanged(object sender, PathChangedArgs e)
        {
            IFluentOperationConfig operation = null;
            expectations.TryRemove(e.OldPath, out operation);

            if (operation != null)
            {
                expectations.TryAdd(e.NewPath, operation);
            }
        }
    }
}
