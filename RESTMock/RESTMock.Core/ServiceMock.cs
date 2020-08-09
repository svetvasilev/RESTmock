using System;
using System.Collections.Concurrent;
using System.Net;
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

        public async void Stop()
        {
            await Task.Run(() => httpListener.Stop());
        }

        public IFluentOperationConfig SetupGet(string path, int expectedNumberOfInvocation=1)
        {
            // TODO: 1. Creates new OperationConfig instance
            // 2. Sets the path of the operation
            // 3. Assingns the event handler to the RequestReceived event
            var getOperationConfig = new OperationConfig("GET"); // TODO: define an enum for the operation types, if no system one exists already
            getOperationConfig.Path(path);
            RequestReceived += getOperationConfig.RequestReceivedHandler;

            expectations.TryAdd(getOperationConfig.ToString(), getOperationConfig);

            return getOperationConfig;
        }
    }
}
