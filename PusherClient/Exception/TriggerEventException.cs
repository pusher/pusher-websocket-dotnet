namespace PusherClient
{
    /// <summary>
    /// Raised when attempting to trigger an event.
    /// </summary>
    public class TriggerEventException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="TriggerEventException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="code">The Pusher error code.</param>
        public TriggerEventException(string message, ErrorCodes code)
            : base(message, code)
        {
        }
    }
}
