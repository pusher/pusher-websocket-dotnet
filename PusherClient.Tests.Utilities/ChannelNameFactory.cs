using System;

namespace PusherClient.Tests.Utilities
{
    public static class ChannelNameFactory
    {
        public static string CreateUniqueChannelName(ChannelTypes channelType = ChannelTypes.Public, string channelNamePostfix = null)
        {
            string channelPrefix;
            switch (channelType)
            {
                case ChannelTypes.Private:
                    channelPrefix = "private-";
                    break;
                case ChannelTypes.Presence:
                    channelPrefix = "presence-";
                    break;
                default:
                    channelPrefix = string.Empty;
                    break;
            }

            var mockChannelName = $"{channelPrefix}myTestChannel{channelNamePostfix}{DateTime.Now.Ticks}";
            return mockChannelName;
        }
    }
}
