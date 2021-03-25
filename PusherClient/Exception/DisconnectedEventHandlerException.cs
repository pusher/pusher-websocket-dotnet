using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the Disconnected delegate raises an unexpected exception.
    /// </summary>
    public class DisconnectedEventHandlerException : EventHandlerException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="DisconnectedEventHandlerException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DisconnectedEventHandlerException(Exception innerException)
            : base($"Error invoking the Disconnected delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.DisconnectedEventHandlerError, innerException)
        {
        }
    }
}
