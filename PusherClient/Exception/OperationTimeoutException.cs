using System;

namespace PusherClient
{
    /// <summary>
    /// Raised when a client timeout occurs waiting for an asynchrounous operation to complete.
    /// </summary>
    public class OperationTimeoutException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="OperationTimeoutException"/> class.
        /// </summary>
        /// <param name="timeoutPeriod">The timeout period.</param>
        /// <param name="operation">Identifies the client operation that timed out.</param>
        public OperationTimeoutException(TimeSpan timeoutPeriod, string operation)
            : base($"Waiting for '{operation}' has timed out after {timeoutPeriod.TotalSeconds:N} second(s).", ErrorCodes.ClientTimeout)
        {
        }
    }
}
