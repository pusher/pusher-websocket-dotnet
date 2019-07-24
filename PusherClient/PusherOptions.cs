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
        public bool Encrypted { get; set; } = false;

        /// <summary>
        /// Gets or set the Authorizer to use
        /// </summary>
        public IAuthorizer Authorizer { get; set; } = null;

        /// <summary>
        /// Gets or sets the Cluster to user for the Host
        /// </summary>
        public string Cluster { get; set; } = "mt1";

        /// <summary>
        /// The Host to use for custom servers to be configurable.
        /// Defaults to ws-{Cluster}.pusher.com
        /// </summary>
        protected string _Host = null;
        public string Host {
            get { return _Host ?? $"ws-{Cluster}.pusher.com"; }
            set { _Host = value; }
        }
    }
}