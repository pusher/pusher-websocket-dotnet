namespace PusherClient
{
    internal class PusherChannelSubscribeEvent : PusherSystemEvent
    {
        public PusherChannelSubscribeEvent(PusherChannelSubscriptionData data)
            : base(Constants.CHANNEL_SUBSCRIBE, data)
        {
        }
    }
}
