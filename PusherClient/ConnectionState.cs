namespace PusherClient
{
    public enum ConnectionState
    {
        Initialized,
        Connecting,
        Connected,
        Unavailable,
        Disconnected,
        WaitingToReconnect
    }
}
