using System;

namespace PusherClient.Tests.Utilities
{
    public static class ChannelNameFactory
    {
        public static string CreateUniqueChannelName(bool privateChannel = false, bool presenceChannel = false, string channelNamePostfix = null)
        {
            var channelPrefix = string.Empty;

            if (privateChannel)
                channelPrefix = "private-";
            else if (presenceChannel)
                channelPrefix = "presence-";

            var mockChannelName = $"{channelPrefix}myTestChannel{channelNamePostfix}{DateTime.Now.Ticks}";
            return mockChannelName;
        }
    }
}
