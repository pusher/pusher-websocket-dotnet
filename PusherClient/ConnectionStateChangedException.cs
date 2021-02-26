using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the delegate Pusher.Error when the delgate Pusher.ConnectionStateChanged raises an unexpected exception.
    /// </summary>
    public class ConnectionStateChangedException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ConnectionStateChangedException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConnectionStateChangedException(Exception innerException)
            : base($"Error invoking delegate Pusher.ConnectionStateChanged:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
        }
    }
}
