namespace PusherClient
{
    /// <summary>
    /// Contract for the authorization of a channel
    /// </summary>
    public interface IAuthorizer
    {
        /// <summary>
        /// Perform the authorization of the channel
        /// </summary>
        /// <param name="channelName">The name of the channel to authorise on</param>
        /// <param name="socketId">The socket ID to use</param>
        /// <returns></returns>
        string Authorize(string channelName, string socketId);
    }
}