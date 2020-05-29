using System.Collections.Generic;

namespace PusherClient
{
    internal interface IPusher
    {
        void ConnectionStateChanged(ConnectionState state);
        void ErrorOccured(PusherException pusherException);

        void EmitPusherEvent(string eventName, PusherEvent data);
        void EmitChannelEvent(string channelName, string eventName, PusherEvent data);
        void AddMember(string channelName, string member);
        void RemoveMember(string channelName, string member);
        void SubscriptionSuceeded(string channelName, string data);
    }
}
