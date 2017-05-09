using System;
using System.Configuration;

namespace PusherClient.Tests.Utilities
{
    public static class Config
    {
        private const string PUSHER_APP_ID = "PUSHER_APP_ID";
        private const string PUSHER_APP_KEY = "PUSHER_APP_KEY";
        private const string PUSHER_APP_SECRET = "PUSHER_APP_SECRET";

        private static string _appId;
        private static string _appKey;
        private static string _appSecret;

        static Config()
        {
            _appId = Environment.GetEnvironmentVariable(PUSHER_APP_ID);
            if (string.IsNullOrEmpty(_appId))
            {
                _appId = ConfigurationManager.AppSettings.Get(PUSHER_APP_ID);
            }

            _appKey = Environment.GetEnvironmentVariable(PUSHER_APP_KEY);
            if (string.IsNullOrEmpty(_appKey))
            {
                _appKey = ConfigurationManager.AppSettings.Get(PUSHER_APP_KEY);
            }

            _appSecret = Environment.GetEnvironmentVariable(PUSHER_APP_SECRET);
            if (string.IsNullOrEmpty(_appSecret))
            {
                _appSecret = ConfigurationManager.AppSettings.Get(PUSHER_APP_SECRET);
            }
        }

        public static string AppId
        {
            get
            {
                return _appId;
            }
            set
            {
                _appId = value;
            }
        }

        public static string AppKey
        {
            get
            {
                return _appKey;
            }
            set
            {
                _appKey = value;
            }
        }

        public static string AppSecret
        {
            get
            {
                return _appSecret;
            }
            set
            {
                _appSecret = value;
            }
        }
    }
}