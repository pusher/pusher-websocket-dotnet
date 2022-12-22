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
        /// <param name="authorizationEndpoint">The authorization endpoint URL.</param>
        /// <param name="socketId">The socket ID used in the authorization attempt.</param>
        public UserAuthenticationFailureException(string message, ErrorCodes code, string authorizationEndpoint, string socketId)
            : base(message, code)
        {
            this.AuthorizationEndpoint = authorizationEndpoint;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="ChannelAuthorizationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="authorizationEndpoint">The authorization endpoint URL.</param>
        /// <param name="socketId">The socket ID used in the authorization attempt.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public UserAuthenticationFailureException(ErrorCodes code, string authorizationEndpoint, string socketId, Exception innerException)
            : base($"Error authenticating user {Environment.NewLine}{innerException.Message}", code, innerException)
        {
            this.AuthorizationEndpoint = authorizationEndpoint;
        }

        /// <summary>
        /// Gets or sets the authorization endpoint URL.
        /// </summary>
        public string AuthorizationEndpoint { get; private set; }
    }
}
