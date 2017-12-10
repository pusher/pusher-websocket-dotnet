using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PusherClient
{
    public delegate void MemberEventHandler(object sender);
    public delegate void MemberAddedEventHandler(object sender, KeyValuePair<string, dynamic> member);
    public class PresenceChannel : PrivateChannel
    {
        public ConcurrentDictionary<string, dynamic> Members = new ConcurrentDictionary<string, dynamic>();

        public event MemberAddedEventHandler MemberAdded;
        public event MemberEventHandler MemberRemoved;

        public PresenceChannel(string channelName, Pusher pusher) : base(channelName, pusher) { }

        #region Internal Methods

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

        #endregion

        #region Private Methods

        private ConcurrentDictionary<string, dynamic> ParseMembersList(string data)
        {
            ConcurrentDictionary<string, dynamic> members = new ConcurrentDictionary<string, dynamic>();

            var dataAsObj = JsonConvert.DeserializeObject<dynamic>(data);
            
            for (int i = 0; i < (int)dataAsObj.presence.count; i++)
            {
                var id = (string)dataAsObj.presence.ids[i];
                var val = (dynamic)dataAsObj.presence.hash[id];
                members[id] = val;
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

        #endregion

    }
}
