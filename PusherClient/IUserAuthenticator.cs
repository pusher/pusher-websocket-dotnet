namespace PusherClient
{
    /// <summary>
    /// Contract for the authentication of a user
    /// </summary>
    public interface IUserAuthenticator
    {
        /// <summary>
        /// Perform the authentication of the user
        /// </summary>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns>The response received from the authentication endpoint.</returns>
        string Authenticate(string socketId);
    }
}