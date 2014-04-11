using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PusherClient
{
    public class HttpAuthorizer: IAuthorizer
    {
        private Uri _authEndpoint;
        public HttpAuthorizer(string authEndpoint)
        {
            _authEndpoint = new Uri(authEndpoint);
        }

        public async Task<string> Authorize(string channelName, string socketId)
        {
            //string authToken = null;

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            string data = String.Format("channel_name={0}&socket_id={1}", channelName, socketId);
            var response = await httpClient.PostAsync(_authEndpoint, new StringContent(data));
            return await response.Content.ReadAsStringAsync();

            //using (var webClient = new System.Net.WebClient())
            //{
            //	string data = String.Format("channel_name={0}&socket_id={1}", channelName, socketId);
            //	webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            //	authToken = webClient.UploadStringAsync(_authEndpoint, "POST", data,);
            //}

            //return authToken;
        }
    }
}
