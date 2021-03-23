namespace PusherClient
{
    internal interface IPusher
    {
        ITraceLogger TraceLogger { get; set; }

        void ChangeConnectionState(ConnectionState state);
        void ErrorOccured(PusherException pusherException);
        void AddMember(string channelName, string member);
        void RemoveMember(string channelName, string member);
        void SubscriptionSuceeded(string channelName, string data);
        void SubscriptionFailed(string channelName, string data);
        IEventBinder GetEventBinder(string eventBinderKey);
        IEventBinder GetChannelEventBinder(string eventBinderKey, string channelName);

    }
}
