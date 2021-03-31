using Newtonsoft.Json;

namespace PusherClient
{
    internal class PusherChannelSubscriptionData
    {
        public PusherChannelSubscriptionData(string channelName)
        {
            this.Channel = channelName;
        }

        [JsonProperty(PropertyName = "channel")]
        public string Channel { get; }
    }
}
