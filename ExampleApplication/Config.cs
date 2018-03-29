using System.Configuration;

namespace ExampleApplication
{
    public static class Config
    {
        private const string PUSHER_APP_KEY = "PUSHER_APP_KEY";

        static Config()
        {
            if (string.IsNullOrWhiteSpace(AppKey))
            {
                AppKey = ConfigurationManager.AppSettings.Get(PUSHER_APP_KEY);
            }
        }

        public static string AppKey { get; }
    }
}