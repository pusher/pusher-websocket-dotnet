using System;

namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher channel that can be subscribed to
    /// </summary>
    public class Channel : EventEmitter
    {
        private readonly ITriggerChannels _pusher;
        private readonly PusherOptions _options;
        private bool _isSubscribed;

        /// <summary>
        /// To be deprecated, please use Pusher.Subscribed.
        /// Fired when the Channel has successfully been subscribed to.
        /// </summary>
        public event SubscriptionEventHandler Subscribed;

        /// <summary>
        /// Gets whether the Channel is currently Subscribed
        /// </summary>
        public bool IsSubscribed => _isSubscribed;

        /// <summary>
        /// Gets the name of the Channel
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the channel type; Public, Private or Presence.
        /// </summary>
        public ChannelTypes ChannelType
        {
            get
            {
                return GetChannelType(this.Name);
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="channelName">The name of the Channel</param>
        /// <param name="pusher">The parent Pusher object</param>
        /// <param name="options">The Pusher options.</param>
        internal Channel(string channelName, ITriggerChannels pusher, PusherOptions options)
        {
            _pusher = pusher;
            _options = options;
            Name = channelName;
        }

        internal virtual void SubscriptionSucceeded(string data)
        {
            if (!_isSubscribed)
            {
                _isSubscribed = true;
                try
                {
                    Subscribed?.Invoke(this);
                }
                catch (Exception error)
                {
                    if (_options.IsTracingEnabled)
                    {
                        Pusher.Trace.TraceInformation($"Error caught invoking delegate Pusher.Error:{Environment.NewLine}{error}");
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribe from the Channel named channel, if currently subscribed
        /// </summary>
        public void Unsubscribe()
        {
            _pusher.Unsubscribe(Name);
            _isSubscribed = false;
        }

        /// <summary>
        /// Trigger this channel with the provided information
        /// </summary>
        /// <param name="eventName">The name of the event to trigger</param>
        /// <param name="obj">The object to send as the payload on the event</param>
        public void Trigger(string eventName, object obj)
        {
            _pusher.Trigger(Name, eventName, obj);
        }

        /// <summary>
        /// Derives the channel type from the channel name.
        /// </summary>
        /// <param name="channelName">The channel name</param>
        /// <returns>The channel type; Public, Private or Presence.</returns>
        internal static ChannelTypes GetChannelType(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }

            ChannelTypes channelType = ChannelTypes.Public;
            if (channelName.StartsWith(Constants.PRIVATE_CHANNEL, StringComparison.OrdinalIgnoreCase))
            {
                channelType = ChannelTypes.Private;
            }
            else if (channelName.StartsWith(Constants.PRESENCE_CHANNEL, StringComparison.OrdinalIgnoreCase))
            {
                channelType = ChannelTypes.Presence;
            }

            return channelType;
        }
    }
}