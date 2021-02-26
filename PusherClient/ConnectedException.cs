using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the delegate Pusher.Error when the delgate Pusher.Connected raises an unexpected exception.
    /// </summary>
    public class ConnectedException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ConnectedException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConnectedException(Exception innerException)
            : base($"Error invoking delegate Pusher.Connected:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
        }
    }
}
