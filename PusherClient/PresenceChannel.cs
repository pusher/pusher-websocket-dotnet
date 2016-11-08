using Newtonsoft.Json;
using System.Collections.Generic;

namespace PusherClient
{
    public delegate void MemberEventHandler(object sender);
    public delegate void MemberAddedEventHandler(object sender, KeyValuePair<string, dynamic> member);
    public class PresenceChannel : PrivateChannel
    {
        public Dictionary<string, dynamic> Members = new Dictionary<string, dynamic>();

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

        #endregion

        #region Private Methods

        private Dictionary<string, dynamic> ParseMembersList(string data)
        {
            Dictionary<string, dynamic> members = new Dictionary<string, dynamic>();

            var dataAsObj = JsonConvert.DeserializeObject<dynamic>(data);
            
            for (int i = 0; i < (int)dataAsObj.presence.count; i++)
            {
                var id = (string)dataAsObj.presence.ids[i];
                var val = (dynamic)dataAsObj.presence.hash[id];
                members.Add(id, val);
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
