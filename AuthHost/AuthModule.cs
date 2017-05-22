using System;
using System.Configuration;
using Nancy;
using PusherServer;

namespace AuthHost
{
    public class AuthModule : NancyModule
    {

        public string PusherApplicationKey => ConfigurationManager.AppSettings["PusherApplicationKey"];

        public string PusherApplicationId => ConfigurationManager.AppSettings["PusherApplicationID"];

        public string PusherApplicationSecret => ConfigurationManager.AppSettings["PusherApplicationSecret"];

        public AuthModule()
        {
            var provider = new Pusher(PusherApplicationId, PusherApplicationKey, PusherApplicationSecret);

            Post["/auth/{username}", ctx => ctx.Request.Form.channel_name && ctx.Request.Form.socket_id] = _ => 
            {
                Console.WriteLine(string.Format("Processing auth request for '{0}' channel, for socket ID '{1}'", Request.Form.channel_name, Request.Form.socket_id));

                string channelName = Request.Form.channel_name;
                string socketId = Request.Form.socket_id;

                string authData = null;

                if (channelName.StartsWith("presence-"))
                {
                    var channelData = new PresenceChannelData
                    {
                        user_id = socketId,
                        user_info = new {name = _.username}
                    };

                    authData = provider.Authenticate(channelName, socketId, channelData).ToJson();
                }
                else
                {
                    authData = provider.Authenticate(channelName, socketId).ToJson();
                }

                return authData;
            };
        }
    }
}