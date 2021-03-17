using System.Threading.Tasks;

namespace PusherClient.Tests.Utilities
{
    public static class PusherFactory
    {
        public static Pusher GetPusher(IAuthorizer authorizer = null)
        {
            PusherOptions options = new PusherOptions()
            {
                Authorizer = authorizer,
                Cluster = Config.Cluster,
                Encrypted = Config.Encrypted,
                IsTracingEnabled = true,
            };

            return new Pusher(Config.AppKey, options);
        }

        public static Pusher GetPusher(ChannelTypes channelType, string username = null)
        {
            switch (channelType)
            {
                case ChannelTypes.Private:
                case ChannelTypes.Presence:
                    return GetPusher(new FakeAuthoriser(username ?? UserNameFactory.CreateUniqueUserName()));
                default:
                    return GetPusher(authorizer: null);
            }
        }

        public static async Task DisposePusherAsync(Pusher pusher)
        {
            if (pusher != null)
            {
                if (pusher.State != ConnectionState.Connected)
                {
                    await pusher.ConnectAsync().ConfigureAwait(false);
                }

                await pusher.UnsubscribeAllAsync().ConfigureAwait(false);
                await pusher.DisconnectAsync().ConfigureAwait(false);
            }
        }
    }
}