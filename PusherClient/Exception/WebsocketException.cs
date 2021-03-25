using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when a <see cref="WebSocket4Net.WebSocket"/> error is emitted.
    /// </summary>
    public class WebsocketException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="WebsocketException"/> class.
        /// </summary>
        /// <param name="state">The state of the connection at the time the error.</param>
        /// <param name="innerException">The exception that is the cause of the error.</param>
        public WebsocketException(ConnectionState state, Exception innerException)
            : base($"{nameof(WebSocket4Net.WebSocket)} error emitted:{Environment.NewLine}{innerException.Message}", ErrorCodes.WebSocketError, innerException)
        {
            this.State = state;
        }

        /// <summary>
        /// Gets the <see cref="ConnectionState"/> at the time of error.
        /// </summary>
        public ConnectionState State { get; private set; }
    }
}
