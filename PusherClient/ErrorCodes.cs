namespace PusherClient
{
    /// <summary>
    /// An enum representing the different error codes. Errors 4000 - 4999 are received back from the Pusher cluster.
    /// Error codes 5000 and above are for errors detected in the client.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown = 0,

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
        ///  An event emitter action error has occured.
        /// </summary>
        EventEmitterActionError = 5100,

        /// <summary>
        /// The presence or private channel has not had its Authorizer set.
        /// </summary>
        ChannelAuthorizerNotSet = 7500,

        /// <summary>
        /// An error was caught when attempting to authorize a presence or private channel. For example; a 404 Not Found HTTP error was raised by the Authorizer.
        /// </summary>
        ChannelAuthorizationError = 7501,

        /// <summary>
        /// The presence or private channel is unauthorized. Received a 403 Forbidden HTTP error from the Authorizer.
        /// </summary>
        ChannelUnauthorized = 7502,

        /// <summary>
        /// An error was caught when emitting an event to the Pusher.Connected event handler.
        /// </summary>
        ConnectedEventHandlerError = 7601,

        /// <summary>
        /// An error was caught when emitting an event to the Pusher.ConnectionStateChanged event handler.
        /// </summary>
        ConnectionStateChangedEventHandlerError = 7602,

        /// <summary>
        /// An error was caught when emitting an event to the Pusher.Disconnected event handler.
        /// </summary>
        DisconnectedEventHandlerError = 7603,

        /// <summary>
        /// An error was caught when emitting an event to the GenericPresenceChannel.MemberAdded event handler.
        /// </summary>
        MemberAddedEventHandlerError = 7604,

        /// <summary>
        /// An error was caught when emitting an event to the GenericPresenceChannel.MemberRemoved event handler.
        /// </summary>
        MemberRemovedEventHandlerError = 7605,

        /// <summary>
        /// An error was caught when emitting an event to the Pusher.Subscribed or Channel.Subscribed event handlers.
        /// </summary>
        SubscribedEventHandlerError = 7606,
    }
}