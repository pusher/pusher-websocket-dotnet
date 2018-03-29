namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher channel that can be subscribed to
    /// </summary>
    public class Channel : EventEmitter
    {
        private readonly ITriggerChannels _pusher;
        private bool _isSubscribed;

        /// <summary>
        /// Fired when the Channel has successfully been subscribed to
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
            if (_isSubscribed)
                return;

            _isSubscribed = true;

            if(Subscribed != null)
                Subscribed(this);
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
    }
}