namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// The test application configuration settings.
    /// </summary>
    public class ApplicationConfig : IApplicationConfig
    {
        /// <summary>
        /// Instantiates an instance of an <see cref="ApplicationConfig"/> class.
        /// </summary>
        public ApplicationConfig()
        {
            this.EnableAuthorizationLatency = true;
        }

        /// <summary>
        /// Gets or sets the Pusher application id.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application key.
        /// </summary>
        public string AppKey { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application secret.
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application cluster.
        /// </summary>
        public string Cluster { get; set; }

        /// <summary>
        /// Gets or sets whether the connection will be encrypted.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets whether an artificial latency is induced when authorizing a channel.
        /// </summary>
        public bool? EnableAuthorizationLatency { get; set; }
    }
}
