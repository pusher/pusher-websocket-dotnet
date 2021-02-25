namespace PusherClient
{
    /// <summary>
    /// The Options to set up the connection with <see cref="Pusher"/>
    /// </summary>
    public class PusherOptions
    {
        /// <summary>
        /// Gets or sets whether the connection will be encrypted
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or set the Authorizer to use
        /// </summary>
        public IAuthorizer Authorizer { get; set; } = null;

        /// <summary>
        /// Gets or sets the Cluster to user for the Host
        /// </summary>
        public string Cluster { get; set; } = "mt1";

        /// <summary>
        /// Gets or sets whether debug tracing is enabled.
        /// </summary>
        public bool IsTracingEnabled { get; set; }

        internal string Host => $"ws-{Cluster}.pusher.com";
    }
}