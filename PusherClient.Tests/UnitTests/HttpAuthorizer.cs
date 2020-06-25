using NUnit.Framework;
using NUnit.Framework.Internal;
using Mock4Net.Core;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class HttpAuthorizer
    {
        [Test]
        public void HttpAuthorizerShouldReturnStringToken()
        {

            int hostPort = 3000;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeTokenAuth = "{auth: 'b928ab800c5c554a47ad:f41b9934520700d8474928d03ea7d808cab0cc7fcec082676f6b73ca0d9ab2b'}";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests.WithUrl("/authz").UsingPost()
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(200)
                        .WithHeader("Content-Type","application/json")
                        .WithBody(FakeTokenAuth)
                );

            var testHttpAuthorizer = new PusherClient.HttpAuthorizer(hostUrl + "/authz");
            var AuthToken = testHttpAuthorizer.Authorize("private-test", "fsfsdfsgsfs");

            server.Stop();

            Assert.AreNotEqual("System.Net.Http.StreamContent", AuthToken);
            Assert.AreEqual(FakeTokenAuth, AuthToken);
        }

        [Test]
        public void HttpAuthorizerWithBearerTokenShouldReturnStringToken()
        {

            int hostPort = 3001;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeBearerToken = "noo6xaeN3cohYoozai4ar8doang7ai1elaeTh1di";
            string FakeTokenAuth = "{auth: 'fohgheoghowi2Zaehai0aixe8as9laiQuahJeez78d03ea7d808cab0cc7fcec082676f6b73ca0d9ab2b'}";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests
                        .WithUrl("/authz").UsingPost()
                        .WithHeader("Authorization", $"Bearer {FakeBearerToken}")
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(200)
                        .WithHeader("Content-Type","application/json")
                        .WithBody(FakeTokenAuth)
                );

            var testHttpAuthorizer = new PusherClient.HttpAuthorizer(hostUrl + "/authz", FakeBearerToken);
            var AuthToken = testHttpAuthorizer.Authorize("private-test", "fsfsdfsgsfs");

            server.Stop();

            Assert.AreNotEqual("System.Net.Http.StreamContent", AuthToken);
            Assert.AreEqual(FakeTokenAuth, AuthToken);
        }
    }
}
