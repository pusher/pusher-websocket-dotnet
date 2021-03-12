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
    }
}