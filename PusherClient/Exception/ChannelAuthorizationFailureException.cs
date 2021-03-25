using System;

namespace PusherClient
{
    /// <summary>
    /// This exception is raised when calling <c>Authorize</c> or <c>AuthorizeAsync</c> on the <see cref="HttpAuthorizer"/>.
    /// </summary>
    public class ChannelAuthorizationFailureException : ChannelException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ChannelAuthorizationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="authorizationEndpoint">The authorization endpoint URL.</param>
        /// <param name="channelName">The name of the channel.</param>
        /// <param name="socketId">The socket ID used in the authorization attempt.</param>
        public ChannelAuthorizationFailureException(string message, ErrorCodes code, string authorizationEndpoint, string channelName, string socketId)
            : base(message, code, channelName, socketId)
        {
            this.AuthorizationEndpoint = authorizationEndpoint;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="ChannelAuthorizationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="authorizationEndpoint">The authorization endpoint URL.</param>
        /// <param name="channelName">The name of the channel.</param>
        /// <param name="socketId">The socket ID used in the authorization attempt.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public ChannelAuthorizationFailureException(ErrorCodes code, string authorizationEndpoint, string channelName, string socketId, Exception innerException)
            : base($"Error authorizing channel {channelName}:{Environment.NewLine}{innerException.Message}", code, channelName, socketId, innerException)
        {
            this.AuthorizationEndpoint = authorizationEndpoint;
        }

        /// <summary>
        /// Gets or sets the authorization endpoint URL.
        /// </summary>
        public string AuthorizationEndpoint { get; private set; }
    }
}
