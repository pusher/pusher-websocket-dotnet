using System;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// Contract for the authentication of a user
    /// </summary>
    public interface IUserAuthenticatorAsync
    {
        /// <summary>
        /// Gets or sets the timeout period for the authenticator. This property is optional.
        /// </summary>
        TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Perform the authentication of the user
        /// </summary>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns>The response received from the authentication endpoint.</returns>
        Task<string> AuthenticateAsync( string socketId);
    }
}