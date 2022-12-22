using System.Collections.Generic;

namespace PusherClient
{
    public class UserEvent
    {
        private readonly Dictionary<string, object> _eventData;
        private readonly string _rawData;

        public UserEvent(Dictionary<string, object> eventData, string rawData)
        {
            _eventData = eventData;
            _rawData = rawData;
        }

        public object GetProperty(string key)
        {
            _eventData.TryGetValue(key, out var value);
            return value;
        }


        public override string ToString()
        {
            return _rawData;
        }
    }
}
