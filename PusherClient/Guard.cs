using System;

namespace PusherClient
{
    internal static class Guard
    {
        internal static void ChannelName(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }
        }

        internal static void EventName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }
        }
    }
}
