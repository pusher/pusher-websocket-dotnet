namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher Private Channel
    /// </summary>
    public class PrivateChannel : Channel
    {
        internal PrivateChannel(string channelName, ITriggerChannels pusher, PusherOptions options) : base(channelName, pusher, options) { }
    }
}