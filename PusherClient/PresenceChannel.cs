using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PusherClient
{
    /// <summary>
    /// Represents a Pusher Presence Channel that can be subscribed to
    /// </summary>
    public class PresenceChannel : PrivateChannel
    {
        /// <summary>
        /// Fires when a Member is Added
        /// </summary>
        public event MemberAddedEventHandler MemberAdded;

        /// <summary>
        /// Fires when a Member is Removed
        /// </summary>
        public event MemberRemovedEventHandler MemberRemoved;

        internal PresenceChannel(string channelName, ITriggerChannels pusher) : base(channelName, pusher) { }

        /// <summary>
        /// Gets the Members of the channel
        /// </summary>
        public ConcurrentDictionary<string, dynamic> Members { get; private set; } = new ConcurrentDictionary<string, dynamic>();

        internal override void SubscriptionSucceeded(string data)
        {
            Members = ParseMembersList(data);
            base.SubscriptionSucceeded(data);
        }

        internal void AddMember(string data)
        {
            var member = ParseMember(data);

            Members[member.Key] = member.Value;

            if (MemberAdded != null)
                MemberAdded(this, member);
        }

        internal void RemoveMember(string data)
        {
            var member = ParseMember(data);

            if (Members.ContainsKey(member.Key))
            {
                dynamic removed;
                if (Members.TryRemove(member.Key, out removed))
                {
                    if (MemberRemoved != null)
                        MemberRemoved(this);
                }
            }
        }

        private ConcurrentDictionary<string, dynamic> ParseMembersList(string data)
        {
            ConcurrentDictionary<string, dynamic> members = new ConcurrentDictionary<string, dynamic>();

            var dataAsObj = JsonConvert.DeserializeObject<dynamic>(data);

            for (int i = 0; i < (int)dataAsObj.presence.count; i++)
            {
                var id = (string)dataAsObj.presence.ids[i];
                var val = (dynamic)dataAsObj.presence.hash[id];
                members[id](val);
            }

            return members;
        }

        private KeyValuePair<string, dynamic> ParseMember(string data)
        {
            var dataAsObj = JsonConvert.DeserializeObject<dynamic>(data);

            var id = (string)dataAsObj.user_id;
            var val = (dynamic)dataAsObj.user_info;

            return new KeyValuePair<string, dynamic>(id, val);
        }
    }
}