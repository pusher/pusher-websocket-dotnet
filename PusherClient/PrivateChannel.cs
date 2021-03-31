namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher Private Channel
    /// </summary>
    public class PrivateChannel : Channel
    {
        internal PrivateChannel(string channelName, ITriggerChannels pusher)
            : base(channelName, pusher)
        {
        }
    }
}