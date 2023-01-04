using System.Collections.Generic;

namespace PusherClient
{
    public class UserEvent
    {
        private readonly string _rawData;

        public UserEvent(string rawData)
        {
            _rawData = rawData;
        }

        public string Data()
        {
            return _rawData;
        }


        public override string ToString()
        {
            return _rawData;
        }
    }
}
