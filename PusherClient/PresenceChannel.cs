namespace PusherClient
{
    /// <summary>
    /// This class exists for backwards compatibility with code that isn't using GenericPresenceChannel.
    /// </summary>
    public class PresenceChannel : GenericPresenceChannel<dynamic>
    {
        internal PresenceChannel(string channelName, ITriggerChannels pusher)
            : base(channelName, pusher)
        {
        }
    }
}