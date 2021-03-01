using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when an unexpected error is raised while trying to reconnect the client.
    /// </summary>
    /// <remarks>
    /// The Pusher client will automatically try to reconnect if a connection is dropped. 
    /// This exception is passed to the Pusher Error delegate if an unexpected error is encountered while trying to reconnect.
    /// </remarks>
    public class ReconnectionException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ReconnectionException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ReconnectionException(Exception innerException)
            : base($"Error trying to reconnect the Pusher client:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
        }
    }
}
