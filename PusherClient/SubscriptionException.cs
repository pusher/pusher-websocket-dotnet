namespace PusherClient
{
    /// <summary>
    /// This exception is raised when a pusher_internal:subscription_error message is received from the Pusher server.
    /// </summary>
    public class SubscriptionException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SubscriptionException"/> class.
        /// </summary>
        /// <param name="channelName">The name of the channel for which the exception occured.</param>
        /// <param name="data">The channel's message data.</param>
        public SubscriptionException(string channelName, string data)
            : base($"Subscription error received on channel {channelName}", ErrorCodes.SubscriptionError)
        {
            this.ChannelName = channelName;
            this.MessageData = data;
        }

        /// <summary>
        /// Gets the name of the channel for which the exception occured.
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// Gets the channel's message data.
        /// </summary>
        public string MessageData { get; private set; }
    }
}
