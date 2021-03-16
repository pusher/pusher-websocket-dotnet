namespace PusherClient
{
    /// <summary>
    /// This exception is raised when calling <c>Authorize</c> or <c>AuthorizeAsync</c> on <see cref="HttpAuthorizer"/> and access to the channel is forbidden (403).
    /// </summary>
    public class ChannelUnauthorizedException : ChannelAuthorizationFailureException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ChannelUnauthorizedException"/> class.
        /// </summary>
        /// <param name="authorizationEndpoint">The authorization endpoint URL.</param>
        /// <param name="channelName">The name of the channel for which access is forbidden.</param>
        /// <param name="socketId">the socket ID used in the authorization attempt.</param>
        public ChannelUnauthorizedException(string authorizationEndpoint, string channelName, string socketId)
            : base($"Unauthorized subscription for channel {channelName}.", ErrorCodes.ChannelUnauthorized, authorizationEndpoint, channelName, socketId)
        {
        }
    }
}
