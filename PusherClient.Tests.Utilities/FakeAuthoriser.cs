using PusherServer;

namespace PusherClient.Tests.Utilities
{
    public class FakeAuthoriser : IAuthorizer
    {
        private readonly string _userName;

        public FakeAuthoriser(string userName)
        {
            _userName = userName;
        }

        public string Authorize(string channelName, string socketId)
        {
            var provider = new PusherServer.Pusher(Config.AppId, Config.AppKey, Config.AppSecret);

            string authData;

            if (channelName.StartsWith("presence-"))
            {
                var channelData = new PresenceChannelData();
                channelData.user_id = socketId;
                channelData.user_info = new { name = _userName };

                authData = provider.Authenticate(channelName, socketId, channelData).ToJson();
            }
            else
            {
                authData = provider.Authenticate(channelName, socketId).ToJson();
            }

            return authData;
        }
    }
}
