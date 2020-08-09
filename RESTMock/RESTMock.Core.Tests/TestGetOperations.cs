using Microsoft.VisualStudio.TestTools.UnitTesting;

using RESTMock.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace RESTMock.Core.Tests
{
    [TestClass]
    public class TestGetOperations
    {
        [TestMethod]
        public async Task PathOnly_HappyPath()
        {
            var serviceMock = new ServiceMock("http://localhost:8088/");

            serviceMock.SetupGet("test/path")
                .ContentType("text\\text")
                .BodyProcessor(rs => ProcessBasicRequest(rs));

            serviceMock.Start();

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://localhost:8088/");
                var response = await httpClient.GetAsync("test/path");

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code is not as expected!");
                // Assert.AreEqual("text\\text", response.Headers.)
                string content = await response.Content.ReadAsStringAsync();

                Assert.AreEqual("Basic test response!", content, "The response content is not as expected!");

            }
        }

        private OperationResponse ProcessBasicRequest(Stream rs)
        {
            var operationresponse = new OperationResponse()
            {
                StatusCode = System.Net.HttpStatusCode.OK
            };

            var memStream = new MemoryStream(256);
            var responseWriter = new StreamWriter(memStream);

            responseWriter.Write("Basic test response!");
            responseWriter.Flush();

            operationresponse.RawBody = memStream;

            return operationresponse;
        }
    }
}
