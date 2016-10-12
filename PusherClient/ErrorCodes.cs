namespace PusherClient
{
    public enum ErrorCodes
    {
        // Catch all
        Unkown = 0,

        // Reserved codes
        MustConnectOverSSL = 4000,
        ApplicationDoesNotExist = 4001,
        ApplicationDisabled = 4003,
        ApplicationOverConnectionQuota = 4004,
        PathNotFound = 4005,
        ClientOverRateLimit = 4301,

        // Library codes
        ConnectionFailed = 5000, 
        ChannelAuthorizerNotSet = 5001,
        NotConnected = 5002,
        SubscriptionError = 5003
    }
}
