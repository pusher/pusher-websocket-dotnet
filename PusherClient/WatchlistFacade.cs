using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PusherClient
{
    internal class WatchlistFacade : EventEmitter<WatchlistEvent>, IWatchlistFacade
    {
        internal void OnPusherEvent(string eventName, PusherEvent pusherEvent) {
            if (eventName == Constants.PUSHER_WATCHLIST_EVENT) {
                List<WatchlistEvent> watchlistEvents = ParseWatchlistEvents(pusherEvent);
                foreach (WatchlistEvent watchlistEvent in watchlistEvents) {
                    EmitEvent(watchlistEvent.Name, watchlistEvent);
                }
            }
        }

        List<WatchlistEvent> ParseWatchlistEvents(PusherEvent pusherEvent) {
            List<WatchlistEvent> watchlistEvents = new List<WatchlistEvent>();
            JObject jObject = JObject.Parse(pusherEvent.Data);
            JToken jToken = jObject.SelectToken("events");
            if (jToken != null)
            {
                if (jToken.Type == JTokenType.Array)
                {
                    JArray eventsJsonArray = jToken.Value<JArray>();
                    foreach (JToken eventJson in eventsJsonArray) {
                        if (eventJson.Type == JTokenType.Object)
                        {
                            string name = null;
                            List<string> userIDs = null;
                            JObject eventJsonObject = eventJson.Value<JObject>();
                            JToken nameToken = eventJsonObject.SelectToken("name");
                            if (nameToken != null && nameToken.Type == JTokenType.String)
                            {
                                name = nameToken.Value<string>();
                            }
                            JToken userIDsToken = eventJsonObject.SelectToken("user_ids");
                            if (userIDsToken != null && userIDsToken.Type == JTokenType.Array)
                            {
                                userIDs = new List<string>();
                                JArray userIDsArray = userIDsToken.Value<JArray>();
                                foreach (JToken userIDToken in userIDsArray) {
                                    if (userIDToken.Type == JTokenType.String)
                                    {
                                        userIDs.Add(userIDToken.Value<string>());
                                    }
                                }
                            }

                            if (name != null && userIDs != null)
                            {
                                watchlistEvents.Add(new WatchlistEvent(name, userIDs, eventJson.ToString()));
                            }
                        }
                    }
                }
            }
            return watchlistEvents;
        }
    }
}