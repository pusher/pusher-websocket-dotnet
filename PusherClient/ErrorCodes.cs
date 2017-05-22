namespace PusherClient
{
    /// <summary>
    /// An Enum representing the different Error codes received back from Pusher
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown - The Catch All error code
        /// </summary>
        Unkown = 0,

        /// <summary>
        /// The connection must be established over SSL
        /// </summary>
        MustConnectOverSSL = 4000,
        /// <summary>
        /// The application does not exist
        /// </summary>
        ApplicationDoesNotExist = 4001,
        /// <summary>
        /// The Application has been disbaled
        /// </summary>
        ApplicationDisabled = 4003,
        /// <summary>
        /// The Application has exceeded its Connection Quota
        /// </summary>
        ApplicationOverConnectionQuota = 4004,
        /// <summary>
        /// The Path was not found
        /// </summary>
        PathNotFound = 4005,
        /// <summary>
        /// The Client has exceeded it Rate Limit
        /// </summary>
        ClientOverRateLimit = 4301,

        /// <summary>
        /// The Channel has not had it's Authorizer set
        /// </summary>
        ChannelAuthorizerNotSet = 5001,

        /// <summary>
        /// No connection is present
        /// </summary>
        NotConnected = 5002,

        /// <summary>
        ///  A Subscription Error has occured
        /// </summary>
        SubscriptionError = 5003
    }
}