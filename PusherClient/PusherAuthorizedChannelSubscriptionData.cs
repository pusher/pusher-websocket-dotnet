using Newtonsoft.Json;

namespace PusherClient
{
    internal class PusherAuthorizedChannelSubscriptionData : PusherChannelSubscriptionData
    {
        public PusherAuthorizedChannelSubscriptionData(string channelName, string auth, string channelData)
            : base(channelName)
        {
            this.Auth = auth;
            this.ChannelData = channelData;
        }

        [JsonProperty(PropertyName = "auth")]
        public string Auth { get; }

        [JsonProperty(PropertyName = "channel_data")]
        public string ChannelData { get; }
    }
}
