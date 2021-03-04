using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the Subscribed delegate raises an unexpected exception.
    /// </summary>
    public class SubscribedException : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SubscribedException"/> class.
        /// </summary>
        /// <param name="channelName">The name of the channel for which the exception occured.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="data">The subscription data.</param>
        public SubscribedException(string channelName, Exception innerException, string data)
            : base($"Error invoking the Pusher Subscribed delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.Unkown, innerException)
        {
            this.ChannelName = channelName;
            this.SubscriptionData = data;
        }

        /// <summary>
        /// Gets the name of the channel for which the exception occured.
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// Gets the data associated with the subscription.
        /// </summary>
        public string SubscriptionData { get; private set; }
    }
}
