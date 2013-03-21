using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace PusherClient
{
    public class HttpAuthorizer: IAuthorizer
    {
        private Uri _authEndpoint;
        public HttpAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        public string Authorize(string channelName, string socketId)
        {
            string authToken = null;

            using (var webClient = new System.Net.WebClient())
            {
                string data = String.Format("channel_name={0}&socket_id={1}", channelName, socketId);
                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                authToken = webClient.UploadString(_authEndpoint, "POST", data);
            }

            return authToken;
        }
    }
}
