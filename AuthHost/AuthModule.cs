using System;
using System.Threading.Tasks;
using Nancy;
using PusherClient;
using PusherClient.Tests.Utilities;
using PusherServer;

namespace AuthHost
{
    public class AuthModule : NancyModule
    {

        public string PusherApplicationKey => Config.AppKey;

        public string PusherApplicationId => Config.AppId;

        public string PusherApplicationSecret => Config.AppSecret;

        public const string EncryptionMasterKeyText = "Rk4twMwEogcmx5dpV+6puT+nNidXoRd3smLvWR57FbQ=";

        public AuthModule()
        {
            PusherServer.PusherOptions options = new PusherServer.PusherOptions
            {
                EncryptionMasterKey = Convert.FromBase64String(EncryptionMasterKeyText),
                Cluster = Config.Cluster,
            };
            var provider = new PusherServer.Pusher(PusherApplicationId, PusherApplicationKey, PusherApplicationSecret, options);

            Post["/auth/{username}", ctx => ctx.Request.Form.channel_name && ctx.Request.Form.socket_id] = _ =>
            {
                Console.WriteLine($"Processing auth request for '{Request.Form.channel_name}' channel, for socket ID '{Request.Form.socket_id}'");

                string channelName = Request.Form.channel_name;
                string socketId = Request.Form.socket_id;

                string authData = null;

                if (Channel.GetChannelType(channelName) == ChannelTypes.Presence)
                {
                    var channelData = new PresenceChannelData
                    {
                        user_id = socketId,
                        user_info = new { Name = _.username.ToString() }
                    };

                    authData = provider.Authenticate(channelName, socketId, channelData).ToJson();
                }
                else
                {
                    authData = provider.Authenticate(channelName, socketId).ToJson();
                }

                if (Channel.GetChannelType(channelName) == ChannelTypes.PrivateEncrypted)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    SendSecretMessageAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }

                return authData;
            };
        }

        private async Task SendSecretMessageAsync()
        {
            await Task.Delay(5000).ConfigureAwait(false);
            PusherServer.PusherOptions options = new PusherServer.PusherOptions
            {
                EncryptionMasterKey = Convert.FromBase64String(EncryptionMasterKeyText),
                Cluster = Config.Cluster,
            };
            string channelName = "private-encrypted-channel";
            string eventName = "secret-event";
            var provider = new PusherServer.Pusher(PusherApplicationId, PusherApplicationKey, PusherApplicationSecret, options);
            string secretMessage = $"sent secret at {DateTime.Now} on '{channelName}' using event '{eventName}'.";
            await provider.TriggerAsync(channelName, eventName, new
            {
                Name = nameof(AuthModule),
                Message = secretMessage,
            }).ConfigureAwait(false);
            Console.WriteLine(secretMessage);
        }
    }
}