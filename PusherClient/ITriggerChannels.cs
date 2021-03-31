using System.Threading.Tasks;

namespace PusherClient
{
    internal interface ITriggerChannels
    {
        Task TriggerAsync(string channelName, string eventName, object obj);

        Task ChannelUnsubscribeAsync(string channelName);

        void RaiseChannelError(PusherException error);
    }
}