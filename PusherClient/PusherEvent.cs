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

        public string UserId
        {
            get
            {
                string result = null;
                if (_eventData.TryGetValue("user_id", out object obj))
                {
                    result = obj.ToString();
                }

                return result;
            }
        }

        public string ChannelName
        {
            get
            {
                string result = null;
                if (_eventData.TryGetValue("channel", out object obj))
                {
                    result = obj.ToString();
                }

                return result;
            }
        }

        public string EventName
        {
            get
            {
                string result = null;
                if (_eventData.TryGetValue("event", out object obj))
                {
                    result = obj.ToString();
                }

                return result;
            }
        }

        public string Data
        {
            get
            {
                string result = null;
                if (_eventData.TryGetValue("data", out object obj))
                {
                    if (obj is string)
                    {
                        result = (string)obj;
                    }
                    else
                    {
                        result = DefaultSerializer.Default.Serialize(obj);
                    }
                }

                return result;
            }
        }

        public override string ToString()
        {
            return _rawEvent;
        }
    }
}
