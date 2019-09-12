using System.Collections.Generic;
using Newtonsoft.Json;

namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher Presence Channel that can be subscribed to
    /// </summary>
    /// <typeparam name="T">Type used to deserialize channel member info</typeparam>
    public class GenericPresenceChannel<T> : PrivateChannel
    {
        /// <summary>
        /// Fires when a Member is Added
        /// </summary>
        public event MemberAddedEventHandler<T> MemberAdded;

        /// <summary>
        /// Fires when a Member is Removed
        /// </summary>
        public event MemberRemovedEventHandler MemberRemoved;

        internal GenericPresenceChannel(string channelName, ITriggerChannels pusher) : base(channelName, pusher) { }

        /// <summary>
        /// Gets the Members of the channel
        /// </summary>
        public Dictionary<string, T> Members { get; private set; } = new Dictionary<string, T>();

        internal override void SubscriptionSucceeded(string data)
        {
            Members = ParseMembersList(data);
            base.SubscriptionSucceeded(data);
        }

        internal void AddMember(string data)
        {
            var member = ParseMember(data);

            if (!Members.ContainsKey(member.Key))
                Members.Add(member.Key, member.Value);
            else
                Members[member.Key] = member.Value;

            if (MemberAdded != null)
                MemberAdded(this, member);
        }

        internal void RemoveMember(string data)
        {
            var member = ParseMember(data);

            if (Members.ContainsKey(member.Key))
            {
                Members.Remove(member.Key);

                if (MemberRemoved != null)
                    MemberRemoved(this);
            }
        }

        private class SubscriptionData
        {
            public Presence presence { get; set; }

            internal class Presence
            {
                public List<string> ids { get; set; }
                public Dictionary<string, T> hash { get; set; }
                public int count { get; set; }
            }
        }

        private Dictionary<string, T> ParseMembersList(string data)
        {
            Dictionary<string, T> members = new Dictionary<string, T>();

            var dataAsObj = JsonConvert.DeserializeObject<SubscriptionData>(data);

            for (int i = 0; i < (int)dataAsObj.presence.count; i++)
            {
                var id = dataAsObj.presence.ids[i];
                var val = dataAsObj.presence.hash[id];
                members.Add(id, val);
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