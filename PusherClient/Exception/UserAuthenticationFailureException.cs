using System;

namespace PusherClient
{
    /// <summary>
    /// This exception is raised when calling <c>Authenticate</c> or <c>AuthenticateAsync</c> on the <see cref="HttpUserAuthneticator"/>.
    /// </summary>
    public class UserAuthenticationFailureException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="UserAuthenticationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="authenticationEndpoint">The user authentication endpoint URL.</param>
        /// <param name="socketId">The socket ID used in the user authentication attempt.</param>
        public UserAuthenticationFailureException(string message, ErrorCodes code, string authenticationEndpoint, string socketId)
            : base(message, code)
        {
            this.AuthenticationEndpoint = authenticationEndpoint;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="UserAuthenticationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="authenticationEndpoint">The user authentication endpoint URL.</param>
        /// <param name="socketId">The socket ID used in the user authentication attempt.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public UserAuthenticationFailureException(ErrorCodes code, string authenticationEndpoint, string socketId, Exception innerException)
            : base($"Error authenticating user {Environment.NewLine}{innerException.Message}", code, innerException)
        {
            this.AuthenticationEndpoint = authenticationEndpoint;
        }

        /// <summary>
        /// Gets or sets the authentication endpoint URL.
        /// </summary>
        public string AuthenticationEndpoint { get; private set; }
    }
}
