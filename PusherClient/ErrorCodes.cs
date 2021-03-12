namespace PusherClient
{
    /// <summary>
    /// An enum representing the different error codes. Errors 4000 - 5003 are received back from the Pusher cluster.
    /// Error codes above 7500 are for errors detected in the client.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unkown = 0,

        /// <summary>
        /// The connection must be established over SSL.
        /// </summary>
        MustConnectOverSSL = 4000,

        /// <summary>
        /// The application does not exist.
        /// </summary>
        ApplicationDoesNotExist = 4001,

        /// <summary>
        /// The application has been disbaled.
        /// </summary>
        ApplicationDisabled = 4003,

        /// <summary>
        /// The application has exceeded its connection quota.
        /// </summary>
        ApplicationOverConnectionQuota = 4004,

        /// <summary>
        /// The path was not found.
        /// </summary>
        PathNotFound = 4005,

        /// <summary>
        /// The client has exceeded it rate limit.
        /// </summary>
        ClientOverRateLimit = 4301,

        /// <summary>
        /// No connection is present.
        /// </summary>
        NotConnected = 5002,

        /// <summary>
        ///  A subscription error has occured.
        /// </summary>
        SubscriptionError = 5003,

        /// <summary>
        /// The presence or private channel has not had its Authorizer set.
        /// </summary>
        ChannelAuthorizerNotSet = 7500,

        /// <summary>
        /// The presence or private channel is unauthorized. Received a 403 Forbidden HTTP error from the Authorizer.
        /// </summary>
        ChannelUnauthorized = 7501,
    }
}