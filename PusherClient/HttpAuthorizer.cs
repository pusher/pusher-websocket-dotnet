using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PusherClient
{
    /// <summary>
    /// An implementation of the <see cref="IAuthorizer"/> using Http
    /// </summary>
    public class HttpAuthorizer: IAuthorizer
    {
        private readonly Uri _authEndpoint;
        private AuthenticationHeaderValue _authenticationHeader;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="authEndpoint">The End point to contact</param>
        /// <param name="bearerToken">Optional bearer token</param>
        public HttpAuthorizer(string authEndpoint, string bearerToken) :
            this(
                authEndpoint,
                !string.IsNullOrEmpty(bearerToken)
                    ? new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken)
                    : null
            )
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="authEndpoint">The End point to contact</param>
        /// <param name="authenticationHeader">(optional) arbitrary authentication header</param>
        public HttpAuthorizer(string authEndpoint, AuthenticationHeaderValue authenticationHeader = null)
        {
            _authEndpoint = new Uri(authEndpoint);
            _authenticationHeader = authenticationHeader;
        }

        /// <summary>
        /// Requests the authorisation of channel name
        /// </summary>
        /// <param name="channelName">The channel name to authorize</param>
        /// <param name="socketId">The socket to use during authorization</param>
        /// <returns>the Authorization token</returns>
        public string Authorize(string channelName, string socketId)
        {
            string authToken = null;

            using (var httpClient = new HttpClient())
            {
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("channel_name", $"{channelName}"),
                    new KeyValuePair<string, string>("socket_id", $"{socketId}")
                };

                HttpContent content = new FormUrlEncodedContent(data);

                if (_authenticationHeader != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = _authenticationHeader;
                }

                var response = httpClient.PostAsync(_authEndpoint, content).Result;
                authToken = response.Content.ReadAsStringAsync().Result;
            }

            return authToken;
        }
    }
}
