using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PusherClient
{
    /// <summary>
    /// An implementation of the <see cref="IAuthorizer"/> using Http
    /// </summary>
    public class HttpAuthorizer: IAuthorizer
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

                var response = httpClient.PostAsync(_authEndpoint, content).Result;
                authToken = response.Content.ToString();
            }

            return authToken;
        }
    }
}