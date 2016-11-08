namespace PusherClient
{
    public enum ConnectionState
    {
        Initialized,
        Connecting,
        Connected,
        Unavailable,
        Failed,
        Disconnected,
        WaitingToReconnect
    }
}
