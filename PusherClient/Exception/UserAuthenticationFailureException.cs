using System;

namespace PusherClient
{
    /// <summary>
    /// This exception is raised when calling <c>Authenticate</c> on the <see cref="HttpUserAuthenticator"/>.
    /// </summary>
    public class UserAuthenticationFailureException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="UserAuthenticationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        public UserAuthenticationFailureException(string message, ErrorCodes code)
            : base(message, code)
        {
        }

        /// <summary>
        /// Creates a new instance of a <see cref="UserAuthenticationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public UserAuthenticationFailureException(string message, ErrorCodes code, Exception innerException)
            : base($"Error authenticating user:{Environment.NewLine}{innerException.Message}", code, innerException)
        {
        }
    }
}
