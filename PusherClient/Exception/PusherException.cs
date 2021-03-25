using System;

namespace PusherClient
{
    /// <summary>
    /// A Pusher Exception
    /// </summary>
    public class PusherException : Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="PusherException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        public PusherException(string message, ErrorCodes code)
            : base(message)
        {
            PusherCode = code;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="PusherException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public PusherException(string message, ErrorCodes code, Exception innerException)
            : base(message, innerException)
        {
            PusherCode = code;
        }

        /// <summary>
        /// Gets the Pusher error code
        /// </summary>
        public ErrorCodes PusherCode { get; }

        /// <summary>
        /// Gets or sets whether this exception has been emitted to the <c>Pusher.Error</c> event handler.
        /// </summary>
        /// <remarks>This property helps prevent duplicate error events from being emitted.</remarks>
        public bool EmittedToErrorHandler { get; set; }
    }
}