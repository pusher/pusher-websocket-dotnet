using Mock4Net.Core;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class HttpChannelAuthorizerTest
    {
        private const int TimeoutRetryAttempts = 5;
        private static int _HostPort = 3000;

        [Test]
        public void HttpChannelAuthorizerShouldReturnStringTokenIfAuthorized()
        {
            int hostPort = _HostPort++;
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
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz")
            {
                Timeout = TimeSpan.FromSeconds(30),
                AuthenticationHeader = new AuthenticationHeaderValue("Authorization", "Bearer noo6xaeN3cohYoozai4ar8doang7ai1elaeTh1di"),
            };
            var AuthToken = testHttpChannelAuthorizer.Authorize("private-test", "fsfsdfsgsfs");

            Assert.AreEqual(FakeTokenAuth, AuthToken);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldReturnStringTokenIfAuthorizedAsync()
        {
            int hostPort = _HostPort++;
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
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz")
            {
                Timeout = TimeSpan.FromSeconds(30),
                AuthenticationHeader = new AuthenticationHeaderValue("Authorization", "Bearer noo6xaeN3cohYoozai4ar8doang7ai1elaeTh1di"),
            };
            var AuthToken = await testHttpChannelAuthorizer.AuthorizeAsync("private-test", "fsfsdfsgsfs").ConfigureAwait(false);

            Assert.AreEqual(FakeTokenAuth, AuthToken);
        }

        [Test]
        public void HttpChannelAuthorizerShouldRaiseExceptionIfUnauthorized()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = _HostPort++;
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
            var testHttpChannelAuthorizer = new PusherClient.HttpChannelAuthorizer(hostUrl + "/unauthz");

            try
            {
                testHttpChannelAuthorizer.Authorize(channelName, socketId);
            }
            catch (ChannelUnauthorizedException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(ChannelUnauthorizedException)}");
            Assert.AreEqual(channelName, exception.ChannelName, nameof(ChannelUnauthorizedException.ChannelName));
            Assert.AreEqual(socketId, exception.SocketID, nameof(ChannelUnauthorizedException.SocketID));
            Assert.AreEqual(ErrorCodes.ChannelUnauthorized, exception.PusherCode);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldRaiseExceptionIfUnauthorizedAsync()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = _HostPort++;
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
            var testHttpChannelAuthorizer = new PusherClient.HttpChannelAuthorizer(hostUrl + "/unauthz");

            try
            {
                await testHttpChannelAuthorizer.AuthorizeAsync(channelName, socketId).ConfigureAwait(false);
            }
            catch (ChannelUnauthorizedException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(ChannelUnauthorizedException)}");
            Assert.AreEqual(channelName, exception.ChannelName, nameof(ChannelUnauthorizedException.ChannelName));
            Assert.AreEqual(socketId, exception.SocketID, nameof(ChannelUnauthorizedException.SocketID));
            Assert.AreEqual(ErrorCodes.ChannelUnauthorized, exception.PusherCode);
        }

        [Test]
        public void HttpChannelAuthorizerShouldRaiseExceptionIfAuthorizerUrlNotFound()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = _HostPort++;
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

            ChannelAuthorizationFailureException exception = null;
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/notfound");

            try
            {
                testHttpChannelAuthorizer.Authorize(channelName, socketId);
            }
            catch (ChannelAuthorizationFailureException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(ChannelAuthorizationFailureException)}");
            Assert.IsTrue(exception.Message.Contains("404"));
            Assert.AreEqual(ErrorCodes.ChannelAuthorizationError, exception.PusherCode);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldRaiseExceptionIfAuthorizerUrlNotFoundAsync()
        {
            string channelName = "private-unauthorized-test";
            string socketId = Guid.NewGuid().ToString("N");
            int hostPort = _HostPort++;
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

            ChannelAuthorizationFailureException exception = null;
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/notfound");

            try
            {
                await testHttpChannelAuthorizer.AuthorizeAsync(channelName, socketId).ConfigureAwait(false);
            }
            catch (ChannelAuthorizationFailureException e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception, $"Expecting a {nameof(ChannelAuthorizationFailureException)}");
            Assert.IsTrue(exception.Message.Contains("404"));
            Assert.AreEqual(ErrorCodes.ChannelAuthorizationError, exception.PusherCode);
        }

        [Test]
        public void HttpChannelAuthorizerShouldRaiseExceptionWhenTimingOut()
        {
            // Arrange
            int hostPort = _HostPort++;
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
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            ChannelAuthorizationFailureException channelException = null;

            // Act
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz") { Timeout = TimeSpan.FromTicks(1), };

            // Try to generate the error multiple times as it does not always error the first time
            for (int attempt = 0; attempt < TimeoutRetryAttempts; attempt++)
            {
                try
                {
                    testHttpChannelAuthorizer.Authorize("private-test", "fsfsdfsgsfs");
                }
                catch (Exception e)
                {
                    channelException = e as ChannelAuthorizationFailureException;
                }

                if (channelException != null && channelException.PusherCode == ErrorCodes.ChannelAuthorizationTimeout)
                {
                    break;
                }
            }

            // Assert
            AssertTimeoutError(channelException);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldRaiseExceptionWhenTimingOutAsync()
        {
            // Arrange
            int hostPort = _HostPort++;
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
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            ChannelAuthorizationFailureException channelException = null;

            // Act
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz") { Timeout = TimeSpan.FromTicks(1), };

            // Try to generate the error multiple times as it does not always error the first time
            for (int attempt = 0; attempt < TimeoutRetryAttempts; attempt++)
            {
                try
                {
                    await testHttpChannelAuthorizer.AuthorizeAsync("private-test", "fsfsdfsgsfs");
                }
                catch (Exception e)
                {
                    channelException = e as ChannelAuthorizationFailureException;
                }

                if (channelException != null && channelException.PusherCode == ErrorCodes.ChannelAuthorizationTimeout)
                {
                    break;
                }
            }

            // Assert
            AssertTimeoutError(channelException);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldRaiseExceptionWhenGatewayTimeouttAsync()
        {
            // Arrange
            int hostPort = _HostPort++;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeTokenAuth = "{auth: 'b928ab800c5c554a47ad:f41b9934520700d8474928d03ea7d808cab0cc7fcec082676f6b73ca0d9ab2b'}";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests.WithUrl("/authz").UsingPost()
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(504)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            ChannelAuthorizationFailureException channelException = null;

            // Act
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz");
            try
            {
                await testHttpChannelAuthorizer.AuthorizeAsync("private-test", "fsfsdfsgsfs");
            }
            catch (Exception e)
            {
                channelException = e as ChannelAuthorizationFailureException;
            }

            // Assert
            AssertTimeoutError(channelException);
        }

        [Test]
        public async Task HttpChannelAuthorizerShouldRaiseExceptionWhenRequestTimeoutAsync()
        {
            // Arrange
            int hostPort = _HostPort++;
            string hostUrl = "http://localhost:" + (hostPort).ToString();
            string FakeTokenAuth = "{auth: 'b928ab800c5c554a47ad:f41b9934520700d8474928d03ea7d808cab0cc7fcec082676f6b73ca0d9ab2b'}";

            var server = FluentMockServer.Start(hostPort);
            server
                .Given(
                    Requests.WithUrl("/authz").UsingPost()
                )
                .RespondWith(
                    Responses
                        .WithStatusCode(408)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(FakeTokenAuth)
                );

            ChannelAuthorizationFailureException channelException = null;

            // Act
            var testHttpChannelAuthorizer = new HttpChannelAuthorizer(hostUrl + "/authz");
            try
            {
                await testHttpChannelAuthorizer.AuthorizeAsync("private-test", "fsfsdfsgsfs");
            }
            catch (Exception e)
            {
                channelException = e as ChannelAuthorizationFailureException;
            }

            // Assert
            AssertTimeoutError(channelException);
        }

        private static void AssertTimeoutError(ChannelAuthorizationFailureException channelException)
        {
            Assert.IsNotNull(channelException, $"Expected a {nameof(ChannelAuthorizationFailureException)}");
            Assert.AreEqual(ErrorCodes.ChannelAuthorizationTimeout, channelException.PusherCode);
        }
    }
}
