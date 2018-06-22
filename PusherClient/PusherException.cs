using System;

namespace PusherClient
{
    /// <summary>
    /// A Pusher Exception
    /// </summary>
    public class PusherException : Exception
    {
        /// <summary>
        /// Gets the Pusher error code
        /// </summary>
        public ErrorCodes PusherCode { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="PusherException"/>
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="code">The Pusher error code</param>
        public PusherException(string message, ErrorCodes code)
            : base(message)
        {
            PusherCode = code;
        }
    }
}