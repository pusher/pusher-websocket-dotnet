using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using PusherRESTDotNet;
using PusherRESTDotNet.Authentication;
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
            var provider = new PusherProvider(PusherApplicationID, PusherApplicationKey, PusherApplicationSecret);

            Post["/auth/{username}"] = parameters =>
            {
                Console.WriteLine(String.Format("Processing auth request for '{0}' channel, for socket ID '{1}'", Request.Form.channel_name, Request.Form.socket_id));

                string channel_name = Request.Form.channel_name;

                if (channel_name.StartsWith("presence-"))
                {
                    var userInfo = new BasicUserInfo();
                    userInfo.name = parameters.username;

                    return provider.Authenticate(Request.Form.channel_name, Request.Form.socket_id,
                        new PusherRESTDotNet.Authentication.PresenceChannelData()
                        {
                            user_id = Request.Form.socket_id,
                            user_info = userInfo
                        });
                }
                else
                {
                    return provider.Authenticate(channel_name, Request.Form.socket_id);
                }
            };
        }
    }
}
