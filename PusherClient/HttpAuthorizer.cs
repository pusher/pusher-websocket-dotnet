using System;
using System.Collections.Generic;
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
        /// Perform the authorization of the channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on.</param>
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="HttpRequestException">If an HTTP call to the authorization URL fails.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public string Authorize(string channelName, string socketId)
        {
            return AuthorizeAsync(channelName, socketId).Result;
        }

        /// <summary>
        /// Perform the authorization of the channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on.</param>
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="HttpRequestException">If an HTTP call to the authorization URL fails.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public async Task<string> AuthorizeAsync(string channelName, string socketId)
        {
            Guard.ChannelName(channelName);

            string authToken = null;
            using (var httpClient = new HttpClient())
            {
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("channel_name", channelName),
                    new KeyValuePair<string, string>("socket_id", socketId),
                };

                using (HttpContent content = new FormUrlEncodedContent(data))
                {
                    HttpResponseMessage response = await httpClient.PostAsync(_authEndpoint, content).ConfigureAwait(false);
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new ChannelUnauthorizedException(channelName, socketId);
                    }

                    response.EnsureSuccessStatusCode();
                    authToken = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            return authToken;
        }
    }
}
