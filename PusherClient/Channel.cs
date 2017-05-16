namespace PusherClient
{
    public delegate void SubscriptionEventHandler(object sender);

    public class Channel : EventEmitter
    {
        private readonly Pusher _pusher;
        private bool _isSubscribed;

        public event SubscriptionEventHandler Subscribed;

        public string Name;

        public bool IsSubscribed => _isSubscribed;

        public Channel(string channelName, Pusher pusher)
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

        public void Unsubscribe()
        {
            _pusher.Unsubscribe(Name);
            _isSubscribed = false;
        }

        public void Trigger(string eventName, object obj)
        {
            _pusher.Trigger(Name, eventName, obj);
        }
    }
}