using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the Connected delegate raises an unexpected exception.
    /// </summary>
    public class ConnectedEventHandlerException : EventHandlerException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ConnectedEventHandlerException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public ConnectedEventHandlerException(Exception innerException)
            : base($"Error invoking the Connected delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.ConnectedEventHandlerError, innerException)
        {
        }
    }
}
