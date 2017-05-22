namespace PusherClient
{
    /// <summary>
    /// An enum for describing the different states the Pusher client can be in
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// The initial state
        /// </summary>
        Uninitialized,
        /// <summary>
        /// The state after the Connection has been initialized
        /// </summary>
        Initialized,
        /// <summary>
        /// The state when the connection process has begun
        /// </summary>
        Connecting,
        /// <summary>
        /// The state when the connection process has cimpleted successfully
        /// </summary>
        Connected,
        /// <summary>
        /// The state when the disconnection process has begun
        /// </summary>
        Disconnecting,
        /// <summary>
        /// The state when the disconnection process has completed, or the connection was dropped
        /// </summary>
        Disconnected,
        /// <summary>
        /// The state when a connectionr etry is in process
        /// </summary>
        WaitingToReconnect
    }
}