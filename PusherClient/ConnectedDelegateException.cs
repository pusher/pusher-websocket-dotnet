using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the Connected delegate raises an unexpected exception.
    /// </summary>
    public class ConnectedDelegateException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ConnectedDelegateException"/> class.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConnectedDelegateException(Exception innerException)
            : base($"Error invoking the Pusher Connected delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
        }
    }
}
