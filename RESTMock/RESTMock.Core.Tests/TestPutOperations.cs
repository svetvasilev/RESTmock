using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using System.Text;

using NUnit.Framework;

namespace RESTMock.Core.Tests
{
    [TestFixture]
    public class TestPutOperations
    {
        [Test]
        public async Task PathOnly_HappyPath()
        {
            var serviceMock = new ServiceMock("http://localhost:8088/");

            serviceMock.SetupPut("test/id")
                .Accepts("application/json")
                .ContentType("application/json")
                .ResponseStatus(System.Net.HttpStatusCode.OK)
                .BodyProcessor(bp => ProcessRawPutRequest(bp));

            serviceMock.Start();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:8088/");
                    var bodyContent = new StringContent(
                        @"{ ""someProp"": ""propVal1"", ""someArray"":[""a1"",""a2"",""a3""]}"
                        , Encoding.UTF8, "application/json");

                    var response = await httpClient.PutAsync("test/id", bodyContent);

                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code is not as expected!");
                    // Assert.AreEqual("text\\text", response.Headers.)
                    string responseContent = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual(@"{""status"":0, ""message"":""Test succeeded""}", responseContent, "The response content is not as expected!");

                }
            }
            finally
            {
                await serviceMock.Stop();
            }
        }

        private string ProcessRawPutRequest(string body)
        {
            Assert.IsNotNull(body, "The body is null");

            Assert.AreEqual(@"{ ""someProp"": ""propVal1"", ""someArray"":[""a1"",""a2"",""a3""]}", body, "The body is not as  expected");

            return @"{""status"":0, ""message"":""Test succeeded""}";

        }
    }
}
