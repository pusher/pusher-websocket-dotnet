namespace PusherClient
{
    public enum ConnectionState
    {
        Initialized,
        Connecting,
        Connected,
        Disconnected,
        WaitingToReconnect
    }
}