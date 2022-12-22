using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// An implementation of the <see cref="IUserAuthenticator"/> using Http
    /// </summary>
    public class HttpUserAuthenticator: IUserAuthenticator, IUserAuthenticatorAsync
    {
        private readonly Uri _authEndpoint;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="authEndpoint">The End point to contact</param>
        public HttpUserAuthenticator(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        /// <summary>
        /// Gets or sets the internal <see cref="HttpClient"/> authentication header.
        /// </summary>
        public AuthenticationHeaderValue AuthenticationHeader { get; set; }

        /// <summary>
        /// Gets or sets the timeout period for the authenticator. If not specified, the default timeout of 100 seconds is used.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Perform the authorization of the channel.
        /// </summary>
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">If an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public string Authenticate(string socketId)
        {
            string result;
            try
            {
                result = AuthenticateAsync(socketId).Result;
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
        /// <param name="socketId">The socket ID to use.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">If an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <returns>The response received from the authorization endpoint.</returns>
        public async Task<string> AuthenticateAsync(string socketId)
        {
            Console.WriteLine("Authenticating...");
            string authToken = null;
            using (var httpClient = new HttpClient())
            {
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("socket_id", socketId),
                };

                using (HttpContent content = new FormUrlEncodedContent(data))
                {
                    HttpResponseMessage response = null;
                    try
                    {
                        PreRequest(httpClient);
                        response = await httpClient.PostAsync(_authEndpoint, content).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ErrorCodes code = ErrorCodes.UserAuthenticationError;
                        if (e is TaskCanceledException)
                        {
                            code = ErrorCodes.UserAuthenticationTimeout;
                        }

                        throw new UserAuthenticationFailureException(code, _authEndpoint.OriginalString, socketId, e);
                    }

                    if (response.StatusCode == HttpStatusCode.RequestTimeout || response.StatusCode == HttpStatusCode.GatewayTimeout)
                    {
                        throw new UserAuthenticationFailureException($"Authentication timeout ({response.StatusCode}).", ErrorCodes.UserAuthenticationTimeout, _authEndpoint.OriginalString, socketId);
                    }
                    else if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new UserAuthenticationFailureException($"Authentication Failed ({response.StatusCode}).", ErrorCodes.UserAuthenticationError, _authEndpoint.OriginalString, socketId);
                    }

                    try
                    {
                        response.EnsureSuccessStatusCode();
                        authToken = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    catch(Exception e)
                    {
                        throw new UserAuthenticationFailureException(ErrorCodes.UserAuthenticationError, _authEndpoint.OriginalString, socketId, e);
                    }
                }
            }

            return authToken;
        }

        /// <summary>
        /// Called before submitting the request to the authentication endpoint.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> used to submit the authentication request.</param>
        public virtual void PreRequest(HttpClient httpClient)
        {
            if (Timeout.HasValue)
            {
                httpClient.Timeout = Timeout.Value;
            }

            if (AuthenticationHeader != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeader;
            }
        }
    }
}
