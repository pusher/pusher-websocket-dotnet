using System.Collections.Generic;

namespace PusherClient
{
    public class PusherEvent
    {
        private readonly Dictionary<string, object> _eventData;
        private readonly string _rawEvent;

        public PusherEvent(Dictionary<string, object> eventData, string rawEvent)
        {
            _eventData = eventData;
            _rawEvent = rawEvent;
        }

        public object GetProperty(string key)
        {
            _eventData.TryGetValue(key, out var value);
            return value;
        }

        public string UserId => (string)_eventData["user_id"];
        public string ChannelName => (string)_eventData["channel"];
        public string EventName => (string)_eventData["event"];
        public string Data => (string)_eventData["data"];

        public override string ToString()
        {
            return _rawEvent;
        }
    }
}
