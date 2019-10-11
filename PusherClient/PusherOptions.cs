using SuperSocket.ClientEngine;
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
        /// Gets or set a IProxyConnector implementation instance may be one of
        /// HttpConnectProxy, Socks4Connector, or Socks5Connector.
        /// </summary>
        /// <remarks>
        /// Interpret first parameter as URL:
        /// 
        /// - `ProxyFactory("ws://ws-mt1.pusher.com/xxx")`
        /// - `ProxyFactory("wss://ws-mt1.pusher.com/xxx")`
        /// 
        /// From CHANNELS PROTOCOL section:
        /// 
        /// - Default WebSocket ports: 80 (ws) or 443 (wss)
        /// - For Silverlight clients ports 4502 (ws) and 4503 (wss) may be used.
        /// </remarks>
        public Func<string, IProxyConnector> ProxyFactory { get; set; }

        internal string Host => $"ws-{Cluster}.pusher.com";
    }
}