namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// The test application configuration settings.
    /// </summary>
    public interface IApplicationConfig
    {
        /// <summary>
        /// Gets or sets the Pusher application id.
        /// </summary>
        string AppId { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application key.
        /// </summary>
        string AppKey { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application secret.
        /// </summary>
        string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the Pusher application cluster.
        /// </summary>
        string Cluster { get; set; }

        /// <summary>
        /// Gets or sets whether the connection will be encrypted.
        /// </summary>
        bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets whether an artificial latency is induced when authorizing a channel.
        /// </summary>
        bool? EnableAuthorizationLatency { get; set; }
    }
}
