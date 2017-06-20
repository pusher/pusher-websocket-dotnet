namespace PusherClient.Tests.Utilities
{
    public static class PusherFactory
    {
        public static Pusher GetPusher(PusherOptions options = null)
        {
            return new Pusher(GetAppKey(), options);
        }

        public static PusherAsync GetPusherAsync(PusherOptions options = null)
        {
            return new PusherAsync(GetAppKey(), options);
        }

        private static string GetAppKey()
        {
            return Config.AppKey;
        }
    }
}