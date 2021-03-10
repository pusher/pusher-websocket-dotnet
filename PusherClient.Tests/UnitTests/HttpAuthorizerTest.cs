using Mock4Net.Core;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class HttpAuthorizerTest
    {
        [Test]
        public void HttpAuthorizerShouldReturnStringTokenIfAuthorized()
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

            Assert.AreEqual(FakeTokenAuth, AuthToken);
        }

        [Test]
        public async Task HttpAuthorizerShouldRaiseExceptionIfUnauthorizedAsync()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = 3001;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeTokenAuth = "Forbidden";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests.WithUrl("/unauthz").UsingPost()
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(403)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            ChannelUnauthorizedException exception = null;
            var testHttpAuthorizer = new PusherClient.HttpAuthorizer(hostUrl + "/unauthz");

            try
            {
                await testHttpAuthorizer.AuthorizeAsync(channelName, socketId).ConfigureAwait(false);
            }
            catch(ChannelUnauthorizedException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(ChannelUnauthorizedException)}");
            Assert.AreEqual(channelName, exception.ChannelName, nameof(ChannelUnauthorizedException.ChannelName));
            Assert.AreEqual(socketId, exception.SocketID, nameof(ChannelUnauthorizedException.SocketID));
        }

        [Test]
        public async Task HttpAuthorizerShouldRaiseExceptionIfAuthorizerUrlNotFoundAsync()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = 3002;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeTokenAuth = "NotFound";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests.WithUrl("/authz").UsingPost()
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            HttpRequestException exception = null;
            var testHttpAuthorizer = new PusherClient.HttpAuthorizer(hostUrl + "/notfound");

            try
            {
                await testHttpAuthorizer.AuthorizeAsync(channelName, socketId).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(HttpRequestException)}");
            Assert.IsTrue(exception.Message.Contains("404"));
        }
    }
}
