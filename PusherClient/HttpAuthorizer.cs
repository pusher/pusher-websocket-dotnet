using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// An implementation of the <see cref="IAuthorizer"/> using Http
    /// </summary>
    public class HttpAuthorizer: IAuthorizer, IAuthorizerAsync
    {
        private readonly Uri _authEndpoint;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="authEndpoint">The End point to contact</param>
        public HttpAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        /// <summary>
        /// Gets or sets the timeout period for the authorizer. If not specified, the default timeout of 100 seconds is used.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Perform the authorization of the channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on.</param>
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">If an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public string Authorize(string channelName, string socketId)
        {
            string result;
            try
            {
                result = AuthorizeAsync(channelName, socketId).Result;
            }
            catch(AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }

            return result;
        }

        /// <summary>
        /// Perform the authorization of the channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on.</param>
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">If an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public async Task<string> AuthorizeAsync(string channelName, string socketId)
        {
            Guard.ChannelName(channelName);

            string authToken = null;
            using (var httpClient = new HttpClient())
            {
                if (Timeout.HasValue)
                {
                    httpClient.Timeout = Timeout.Value;
                }

                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("channel_name", channelName),
                    new KeyValuePair<string, string>("socket_id", socketId),
                };

                using (HttpContent content = new FormUrlEncodedContent(data))
                {
                    HttpResponseMessage response;
                    try
                    {
                        response = await httpClient.PostAsync(_authEndpoint, content).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ErrorCodes code = ErrorCodes.ChannelAuthorizationError;
                        if (e is TaskCanceledException)
                        {
                            code = ErrorCodes.ChannelAuthorizationTimeout;
                        }

                        throw new ChannelAuthorizationFailureException(code, _authEndpoint.OriginalString, channelName, socketId, e);
                    }

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new ChannelUnauthorizedException(_authEndpoint.OriginalString, channelName, socketId);
                    }

                    try
                    {
                        response.EnsureSuccessStatusCode();
                        authToken = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    catch(Exception e)
                    {
                        throw new ChannelAuthorizationFailureException(ErrorCodes.ChannelAuthorizationError, _authEndpoint.OriginalString, channelName, socketId, e);
                    }
                }
            }

            return authToken;
        }
    }
}
