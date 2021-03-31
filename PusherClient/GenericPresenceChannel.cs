using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher Presence Channel that can be subscribed to.
    /// </summary>
    /// <typeparam name="T">Type used to deserialize channel member detail.</typeparam>
    public class GenericPresenceChannel<T> : PrivateChannel, IPresenceChannel<T>, IPresenceChannelManagement
    {
        /// <summary>
        /// Fires when a Member is Added
        /// </summary>
        public event MemberAddedEventHandler<T> MemberAdded;

        /// <summary>
        /// Fires when a Member is Removed
        /// </summary>
        public event MemberRemovedEventHandler<T> MemberRemoved;

        internal GenericPresenceChannel(string channelName, ITriggerChannels pusher) : base(channelName, pusher)
        {
        }

        /// <summary>
        /// Gets the Members of the channel
        /// </summary>
        private ConcurrentDictionary<string, T> Members { get; set; } = new ConcurrentDictionary<string, T>();

        /// <summary>
        /// Gets a member using the member's user ID.
        /// </summary>
        /// <param name="userId">The member's user ID.</param>
        /// <returns>Retruns the member if found; otherwise returns null.</returns>
        public T GetMember(string userId)
        {
            T result = default;
            if (Members.TryGetValue(userId, out T member))
            {
                result = member;
            }

            return result;
        }

        /// <summary>
        /// Gets the current list of members as a <see cref="Dictionary{TKey, TValue}"/> where the TKey is the user ID and TValue is the member detail.
        /// </summary>
        /// <returns>Returns a <see cref="Dictionary{TKey, TValue}"/> containing the current members.</returns>
        public Dictionary<string, T> GetMembers()
        {
            Dictionary<string, T> result = new Dictionary<string, T>(Members.Count);
            foreach (var member in Members)
            {
                result.Add(member.Key, member.Value);
            }

            return result;
        }

        internal override void SubscriptionSucceeded(string data)
        {
            if (!IsSubscribed)
            {
                Members = ParseMembersList(data);
                base.SubscriptionSucceeded(data);
            }
        }

        void IPresenceChannelManagement.AddMember(string data)
        {
            var member = ParseMember(data);
            Members[member.Key] = member.Value;
            if (MemberAdded != null)
            {
                try
                {
                    MemberAdded.Invoke(this, member);
                }
                catch(Exception e)
                {
                    _pusher.RaiseChannelError(new MemberAddedEventHandlerException<T>(member.Key, member.Value, e));
                }
            }
        }

        void IPresenceChannelManagement.RemoveMember(string data)
        {
            var parsedMember = ParseMember(data);
            if (Members.TryRemove(parsedMember.Key, out T member))
            {
                if (MemberRemoved != null)
                {
                    try
                    {
                        MemberRemoved.Invoke(this, new KeyValuePair<string, T>(parsedMember.Key, member));
                    }
                    catch (Exception e)
                    {
                        _pusher.RaiseChannelError(new MemberRemovedEventHandlerException<T>(parsedMember.Key, member, e));
                    }
                }
            }
        }

        private class SubscriptionData
        {
            public Presence presence { get; set; }

            internal class Presence
            {
                public List<string> ids { get; set; }
                public Dictionary<string, T> hash { get; set; }
            }
        }

        private ConcurrentDictionary<string, T> ParseMembersList(string data)
        {
            JToken jToken = JToken.Parse(data);
            JObject jObject = JObject.Parse(jToken.ToString());

            ConcurrentDictionary<string, T> members = new ConcurrentDictionary<string, T>();

            var dataAsObj = JsonConvert.DeserializeObject<SubscriptionData>(jObject.ToString(Formatting.None));

            for (int i = 0; i < dataAsObj.presence.ids.Count; i++)
            {
                string id = dataAsObj.presence.ids[i];
                T val = dataAsObj.presence.hash[id];
                members[id] = val;
            }

            return members;
        }

        private class MemberData
        {
            public string user_id { get; set; }
            public T user_info { get; set; }
        }

        private KeyValuePair<string, T> ParseMember(string data)
        {
            var dataAsObj = JsonConvert.DeserializeObject<MemberData>(data);

            var id = dataAsObj.user_id;
            var val = dataAsObj.user_info;

            return new KeyValuePair<string, T>(id, val);
        }
    }
}