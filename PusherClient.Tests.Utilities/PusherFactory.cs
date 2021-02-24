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
            };

            return new Pusher(Config.AppKey, options);
        }
    }
}