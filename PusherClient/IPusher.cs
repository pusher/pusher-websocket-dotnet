using System.Collections.Generic;

namespace PusherClient
{
    internal interface IPusher
    {
        void ConnectionStateChanged(ConnectionState state);
        void ErrorOccured(PusherException pusherException);

        void EmitPusherEvent(string eventName, string data);
        void EmitChannelEvent(string channelName, string eventName, string data);
        void AddMember(string channelName, string member);
        void RemoveMember(string channelName, string member);
        void SubscriptionSuceeded(string channelName, string data);
    }
}
