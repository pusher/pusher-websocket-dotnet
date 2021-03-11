using System;
using System.Threading;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher channel that can be subscribed to
    /// </summary>
    public class Channel : EventEmitter
    {
        private readonly ITriggerChannels _pusher;

        /// <summary>
        /// Fired when the Channel has successfully been subscribed to.
        /// </summary>
        internal event SubscriptionEventHandler Subscribed;

        /// <summary>
        /// Gets whether the Channel is currently Subscribed
        /// </summary>
        public bool IsSubscribed { get; internal set; }

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

        internal SemaphoreSlim _subscribeLock = new SemaphoreSlim(1);
        internal SemaphoreSlim _subscribeCompleted;
        internal Exception _subscriptionError;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="channelName">The name of the Channel</param>
        /// <param name="pusher">The parent Pusher object</param>
        internal Channel(string channelName, ITriggerChannels pusher)
        {
            _pusher = pusher;
            Name = channelName;
        }

        internal virtual void SubscriptionSucceeded(string data)
        {
            if (!IsSubscribed)
            {
                IsSubscribed = true;
                try
                {
                    Subscribed?.Invoke(this);
                }
                catch (Exception error)
                {
                    _pusher.RaiseSubscribedError(new SubscribedDelegateException(this, error, data));
                }
            }
        }

        /// <summary>
        /// Unsubscribe from the Channel named channel, if currently subscribed
        /// </summary>
        public void Unsubscribe()
        {
            Task.WaitAll(_pusher.SendUnsubscribe(this));
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
            Guard.ChannelName(channelName);

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