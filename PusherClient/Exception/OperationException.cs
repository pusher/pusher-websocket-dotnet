using System;

namespace PusherClient
{
    /// <summary>
    /// Raised when a client operation generates an unexpected error.
    /// </summary>
    public class OperationException : PusherException
    {
        /// <summary>
        /// Creates a new instance of an <see cref="OperationException"/> class.
        /// </summary>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="operation">Identifies the client operation that errored.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public OperationException(ErrorCodes code, string operation, Exception innerException)
            : base($"An unexpected error was detected when performing the operation '{operation}':{Environment.NewLine}{innerException.Message}", code, innerException)
        {
        }
    }
}
