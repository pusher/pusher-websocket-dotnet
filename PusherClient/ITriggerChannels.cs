using System.Threading.Tasks;

namespace PusherClient
{
    internal interface ITriggerChannels
    {
        Task Trigger(string channelName, string eventName, object obj);

        Task Unsubscribe(string channelName);
    }
}