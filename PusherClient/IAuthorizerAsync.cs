using System;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// Contract for the async authorization of a channel
    /// </summary>
    public interface IAuthorizerAsync
    {
        /// <summary>
        /// Gets or sets the timeout period for the authorizer. This property is optional.
        /// </summary>
        TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Perform the authorization of the channel
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on</param>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns>The response received from the authorization endpoint.</returns>
        Task<string> AuthorizeAsync(string channelName, string socketId);
    }
}