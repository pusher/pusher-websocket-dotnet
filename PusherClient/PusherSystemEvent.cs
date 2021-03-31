using Newtonsoft.Json;

namespace PusherClient
{
    internal class PusherSystemEvent
    {
        public PusherSystemEvent(string eventName, object data)
        {
            this.Event = eventName;
            this.Data = data;
        }

        [JsonProperty(PropertyName = "event")]
        public string Event { get; }

        [JsonProperty(PropertyName = "data")]
        public object Data { get; }
    }
}
