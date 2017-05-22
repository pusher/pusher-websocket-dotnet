namespace PusherClient
{
    internal interface ITriggerChannels
    {
        void Trigger(string channelName, string eventName, object obj);

        void Unsubscribe(string channelName);
    }
}