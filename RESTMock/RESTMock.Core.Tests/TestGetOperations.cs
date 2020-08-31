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
                .ResponseStatus(HttpStatusCode.OK)
                .BodyProcessor(rs => ProcessBasicRequest((string)rs));

            serviceMock.Start();

            try
            {
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
            finally
            {
                await serviceMock.Stop();
            }
            
        }

        [TestMethod]
        public async Task PathOnly_Http403_HappyPath()
        {
            var serviceMock = new ServiceMock("http://localhost:8088/");

            serviceMock.SetupGet("test/path")
                .ContentType("text\\text")
                .ResponseStatus(HttpStatusCode.Forbidden)
                .BodyProcessor(rs => ProcessBasicRequest((string)rs));

            serviceMock.Start();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:8088/");
                    var response = await httpClient.GetAsync("test/path");

                    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "The status code is not as expected!");
                    // Assert.AreEqual("text\\text", response.Headers.)
                    string content = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual("Basic test response!", content, "The response content is not as expected!");

                }
            }
            finally
            {
                await serviceMock.Stop();
            }
        }

        [TestMethod]
        public async Task PathAndQueryParam_HappyPath()
        {
            var serviceMock = new ServiceMock("http://localhost:8088/");

            serviceMock.SetupGet("test/path")
                .QueryParam("param","123")
                .ContentType("text\\text")
                .BodyProcessor(rs => ProcessBasicRequest((string)rs));

            serviceMock.Start();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:8088/");
                    var response = await httpClient.GetAsync("test/path?param=123");

                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code is not as expected!");
                    // Assert.AreEqual("text\\text", response.Headers.)
                    string content = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual("Basic test response!", content, "The response content is not as expected!");

                }
            }
            finally
            {
                await serviceMock.Stop();
            }            
        }

        [TestMethod]
        public async Task PathOnly_DynamicResponse_HappyPath()
        {
            var serviceMock = new ServiceMock("http://localhost:8088/");

            serviceMock.SetupGet("test/path")
                .ContentType("text\\json")
                .ResponseStatus(HttpStatusCode.OK)
                .ResponseBody(() => new OperationResponse<dynamic> { 
                    Body = new { 
                        SomeValue1 = "SomeValue1", 
                        SomeValue2 = "SomeValue2" 
                    }
                });

            serviceMock.Start();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:8088/");
                    var response = await httpClient.GetAsync("test/path");

                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code is not as expected!");
                    // Assert.AreEqual("text\\text", response.Headers.)
                    string content = await response.Content.ReadAsStringAsync();

                    var responseObj = Newtonsoft.Json.Linq.JObject.Parse(content);

                    Assert.AreEqual("SomeValue1", responseObj["SomeValue1"], "The response content is not as expected!");
                    Assert.AreEqual("SomeValue2", responseObj["SomeValue2"], "The response content is not as expected!");

                }
            }
            finally
            {
                await serviceMock.Stop();
            }

        }

        private StringOperationResponse ProcessBasicRequest(string rs)
        {
            var operationresponse = new StringOperationResponse()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Body = "Basic test response!"
            };           

            return operationresponse;
        }
    }
}
