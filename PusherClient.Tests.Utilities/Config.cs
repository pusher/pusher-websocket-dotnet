using System;
using System.Configuration;

namespace PusherClient.Tests.Utilities
{
    public static class Config
    {
        private const string PUSHER_APP_ID = "PUSHER_APP_ID";
        private const string PUSHER_APP_KEY = "PUSHER_APP_KEY";
        private const string PUSHER_APP_SECRET = "PUSHER_APP_SECRET";

        static Config()
        {
            AppId = Environment.GetEnvironmentVariable(PUSHER_APP_ID);
            if (string.IsNullOrWhiteSpace(AppId))
            {
                AppId = ConfigurationManager.AppSettings.Get(PUSHER_APP_ID);
            }

            AppKey = Environment.GetEnvironmentVariable(PUSHER_APP_KEY);
            if (string.IsNullOrWhiteSpace(AppKey))
            {
                AppKey = ConfigurationManager.AppSettings.Get(PUSHER_APP_KEY);
            }

            AppSecret = Environment.GetEnvironmentVariable(PUSHER_APP_SECRET);
            if (string.IsNullOrWhiteSpace(AppSecret))
            {
                AppSecret = ConfigurationManager.AppSettings.Get(PUSHER_APP_SECRET);
            }
        }

        public static string AppId { get; set; }

        public static string AppKey { get; set; }

        public static string AppSecret { get; set; }
    }
}
