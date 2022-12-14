namespace PusherClient
{
    /// <summary>
    /// Contract for the user authentication
    /// </summary>
    public interface IUserAuthenticator
    {
        /// <summary>
        /// Perform the the user authentication
        /// </summary>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns></returns>
        string Authenticate(string socketId);
    }
}