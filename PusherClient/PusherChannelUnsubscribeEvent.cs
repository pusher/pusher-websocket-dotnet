namespace PusherClient
{
    internal class PusherChannelUnsubscribeEvent : PusherSystemEvent
    {
        public PusherChannelUnsubscribeEvent(string channelName)
            : base(Constants.CHANNEL_UNSUBSCRIBE, new PusherChannelSubscriptionData(channelName))
        {
        }
    }
}
