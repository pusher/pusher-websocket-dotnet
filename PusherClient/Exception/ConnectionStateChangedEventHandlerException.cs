using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the ConnectionStateChanged delegate raises an unexpected exception.
    /// </summary>
    public class ConnectionStateChangedEventHandlerException : EventHandlerException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ConnectionStateChangedEventHandlerException"/> class.
        /// </summary>
        /// <param name="state">The state passed to the delegate ConnectionStateChanged at the time of the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConnectionStateChangedEventHandlerException(ConnectionState state, Exception innerException)
            : base($"Error invoking the ConnectionStateChanged delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.ConnectionStateChangedEventHandlerError, innerException)
        {
            this.State = state;
        }

        /// <summary>
        /// Gets the <see cref="ConnectionState"/> change that caused the error.
        /// </summary>
        public ConnectionState State { get; private set; }
    }
}
