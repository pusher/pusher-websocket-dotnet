using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// Contract for the async authorization of a channel
    /// </summary>
    public interface IAuthorizerAsync
    {
        /// <summary>
        /// Perform the authorization of the channel
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on</param>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns>The response received from the authorization endpoint.</returns>
        Task<string> AuthorizeAsync(string channelName, string socketId);
    }
}