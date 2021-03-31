using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets emitted to the Pusher Error delegate whenever an event handler raises an unexpected exception.
    /// </summary>
    public class EventHandlerException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="EventHandlerException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public EventHandlerException(string message, ErrorCodes code, Exception innerException)
            : base(message, code, innerException)
        {
        }
    }
}
