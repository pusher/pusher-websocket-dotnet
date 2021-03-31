namespace PusherClient
{
    internal interface IPresenceChannelManagement
    {
        void AddMember(string data);

        void RemoveMember(string data);
    }
}