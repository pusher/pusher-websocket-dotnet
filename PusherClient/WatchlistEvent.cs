using System.Collections.Generic;

namespace PusherClient
{
    public class WatchlistEvent
    {
        private readonly string _rawData;
        public string Name { get; private set; }
        public List<string> UserIDs { get; private set; }

        public WatchlistEvent(string name, List<string> userIDs, string rawData)
        {
            Name = name;
            UserIDs = userIDs;
            _rawData = rawData;
        }

        public override string ToString()
        {
            return _rawData;
        }
    }
}
