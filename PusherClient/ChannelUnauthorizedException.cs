namespace PusherClient
{
    /// <summary>
    /// This exception is raised when calling <c>Authorize</c> or <c>AuthorizeAsync</c> on <see cref="HttpAuthorizer"/> and access to the channel is forbidden (403).
    /// </summary>
    public class ChannelUnauthorizedException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ChannelUnauthorizedException"/> class.
        /// </summary>
        /// <param name="channelName">The name of the channel for which access is forbidden.</param>
        /// <param name="socketId">the socket ID used in the authorization attempt.</param>
        public ChannelUnauthorizedException(string channelName, string socketId)
            : base($"The channel subscription is unauthorized.", ErrorCodes.ChannelUnauthorized)
        {
            this.ChannelName = channelName;
            this.SocketID = socketId;
        }

        /// <summary>
        /// Gets the name of the channel for which the exception occured.
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// Gets the socket ID used in the authorization attempt.
        /// </summary>
        public string SocketID { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Channel"/> that failed authorization. Note that this value can be null.
        /// </summary>
        public Channel Channel { get; set; }
    }
}
