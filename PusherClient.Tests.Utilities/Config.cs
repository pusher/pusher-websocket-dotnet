namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// Contains the default test configuration.
    /// </summary>
    public static class Config
    {
        static Config()
        {
            IApplicationConfig config = EnvironmentVariableConfigLoader.Default.Load();
            if (string.IsNullOrWhiteSpace(config.AppKey))
            {
                config = JsonFileConfigLoader.Default.Load();
            }

            AppId = config.AppId;
            AppKey = config.AppKey;
            AppSecret = config.AppSecret;
            Cluster = config.Cluster;
            Encrypted = config.Encrypted;
            EnableAuthorizationLatency = config.EnableAuthorizationLatency ?? true;
        }

        /// <summary>
        /// Gets or sets the Pusher application id.
        /// </summary>
        public static string AppId { get; private set; }

        /// <summary>
        /// Gets or sets the Pusher application key.
        /// </summary>
        public static string AppKey { get; private set; }

        /// <summary>
        /// Gets or sets the Pusher application secret.
        /// </summary>
        public static string AppSecret { get; private set; }

        /// <summary>
        /// Gets or sets the Pusher application cluster.
        /// </summary>
        public static string Cluster { get; private set; }

        /// <summary>
        /// Gets or sets whether the connection will be encrypted.
        /// </summary>
        public static bool Encrypted { get; private set; }

        /// <summary>
        /// Gets or sets whether an artificial latency is induced when authorizing a channel.
        /// </summary>
        public static bool EnableAuthorizationLatency { get; set; }

        /// <summary>
        /// Gets the PusherServer HTTP host.
        /// </summary>
        public static string HttpHost
        {
            get
            {
                return $"api-{Cluster}.pusher.com";
            }
        }
    }
}
