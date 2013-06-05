using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using PusherServer;
using System.Configuration;

namespace AuthHost
{
    public class AuthModule : NancyModule
    {

        #region Properties

        public string PusherApplicationKey 
        {
            get
            {
                return ConfigurationManager.AppSettings["PusherApplicationKey"];
            }
        }

        public string PusherApplicationID
        {
            get
            {
                return ConfigurationManager.AppSettings["PusherApplicationID"];
            }
        }

        public string PusherApplicationSecret
        {
            get
            {
                return ConfigurationManager.AppSettings["PusherApplicationSecret"];
            }
        }

        #endregion

        public AuthModule()
        {
            var provider = new Pusher(PusherApplicationID, PusherApplicationKey, PusherApplicationSecret);

            Post["/auth/{username}"] = parameters =>
            {
                Console.WriteLine(String.Format("Processing auth request for '{0}' channel, for socket ID '{1}'", Request.Form.channel_name, Request.Form.socket_id));

                string channel_name = Request.Form.channel_name;
                string socket_id = Request.Form.socket_id;

                if (channel_name.StartsWith("presence-"))
                {
                    var channelData = new PresenceChannelData();
                    channelData.user_id = socket_id;
                    channelData.user_info = new { name = parameters.name };

                    return provider.Authenticate(channel_name, socket_id, channelData).ToJson();
                }
                else
                {
                    return provider.Authenticate(channel_name, Request.Form.socket_id);
                }
            };
        }
    }
}
