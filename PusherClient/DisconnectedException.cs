using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the delegate Pusher.Error when the delgate Pusher.Disconnected raises an unexpected exception.
    /// </summary>
    public class DisconnectedException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="DisconnectedException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DisconnectedException(Exception innerException)
            : base($"Error invoking delegate Pusher.Disconnected:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
        }
    }
}
