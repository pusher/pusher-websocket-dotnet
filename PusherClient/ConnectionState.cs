namespace PusherClient
{
    public enum ConnectionState
    {
        Uninitialized,
        Initialized,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        WaitingToReconnect
    }
}