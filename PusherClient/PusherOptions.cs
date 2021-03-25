using System;

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
        /// Gets or sets the timeout period to wait for an asynchrounous operation to complete. The default value is 30 seconds.
        /// </summary>
        public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the <see cref="ITraceLogger"/> to use for tracing debug messages.
        /// </summary>
        public ITraceLogger TraceLogger { get; set; }

        internal string Host => $"ws-{Cluster}.pusher.com";

        /// <summary>
        /// Gets a timeout 10% less than <c>ClientTimeout</c>. This value is used for inner timeouts.
        /// </summary>
        internal TimeSpan InnerClientTimeout
        {
            get
            {
                return TimeSpan.FromTicks(ClientTimeout.Ticks - ClientTimeout.Ticks / 10);
            }
        }
    }
}