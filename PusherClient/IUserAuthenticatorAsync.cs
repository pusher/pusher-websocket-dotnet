using System;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// Contract for the user async authentication
    /// </summary>
    public interface IUserAuthenticatorAsync
    {
        /// <summary>
        /// Gets or sets the timeout period for the authenticator. This property is optional.
        /// </summary>
        TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Perform the the user  async authentication
        /// </summary>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns>The response received from the authorization endpoint.</returns>
        Task<string> AuthenticateAsync(string socketId);
    }
}