using System;
using System.Security.Authentication;

namespace PusherClient
{
    /// <summary>
    /// The Options to set up the connection with <see cref="Pusher"/>
    /// </summary>
    public class PusherOptions
    {
        private string _cluster;
        private string _host;

        /// <summary>
        /// Instantiates an instance of a <see cref="PusherOptions"/> object.
        /// </summary>
        public PusherOptions()
        {
            Cluster = "mt1";
        }

        /// <summary>
        /// Gets or sets whether the connection will be encrypted.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// DEPRECATED: Use <see cref="ChannelAuthorizer"/> instead.
        /// Gets or set the <see cref="IAuthorizer"/> to use.
        /// </summary>
        public IAuthorizer Authorizer { 
            get
            {
                return (IAuthorizer) ChannelAuthorizer;
            }
            set
            {
                ChannelAuthorizer = value;
            }
        }

        /// <summary>
        /// Gets or set the <see cref="IChannelAuthorizer"/> to use.
        /// </summary>
        public IChannelAuthorizer ChannelAuthorizer { get; set; } = null;

        /// <summary>
        /// Gets or set the <see cref="IUserAuthenticator"/> to use.
        /// </summary>
        public IUserAuthenticator UserAuthenticator { get; set; } = null;

        /// <summary>
        /// Gets or sets the cluster to use for the host.
        /// </summary>
        public string Cluster
        {
            get
            {
                return _cluster;
            }

            set
            {
                if (_cluster != value)
                {
                    _cluster = value;
                    if (_cluster != null)
                    {
                        _host = $"ws-{_cluster}.pusher.com";
                    }
                    else
                    {
                        _host = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the the host to use. For example; ws-some-server:8086.
        /// </summary>
        /// <remarks>
        /// This value will override the <c>Cluster</c> property. This property should only be used in advanced scenarios.
        /// Use the <c>Cluster</c> property instead to define the Pusher server host.
        /// </remarks>
        public string Host
        {
            get
            {
                return _host;
            }

            set
            {
                if (_host != value)
                {
                    _host = value;
                    _cluster = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the timeout period to wait for an asynchrounous operation to complete. The default value is 30 seconds.
        /// </summary>
        public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(30);


        /// <summary>
        /// Gets or sets the ssl security protocol to communicate. The default value is SslProtocols.None.
        /// </summary>
        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// Gets or sets the <see cref="ITraceLogger"/> to use for tracing debug messages.
        /// </summary>
        public ITraceLogger TraceLogger { get; set; }

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