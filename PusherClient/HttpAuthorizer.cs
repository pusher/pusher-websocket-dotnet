using System;
using System.Net;

namespace PusherClient
{
    public class HttpAuthorizer: IAuthorizer
    {
        private readonly Uri _authEndpoint;

        public HttpAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        public string Authorize(string channelName, string socketId)
        {
            string authToken = null;

            using (var webClient = new WebClient())
            {
                var data = $"channel_name={channelName}&socket_id={socketId}";
                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                authToken = webClient.UploadString(_authEndpoint, "POST", data);
            }

            return authToken;
        }
    }
}