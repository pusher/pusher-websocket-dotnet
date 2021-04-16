using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PusherClient
{
    /// <summary>
    /// The Pusher Client object
    /// </summary>
    public class Pusher : EventEmitter, IPusher, ITriggerChannels
    {
        /// <summary>
        /// Fires when a connection has been established with the Pusher Server
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// Fires when the connection is disconnection from the Pusher Server
        /// </summary>
        public event ConnectedEventHandler Disconnected;

        /// <summary>
        /// Fires when the connection state changes
        /// </summary>
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;

        /// <summary>
        /// Fires when an error occurs.
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// Fires when a channel becomes subscribed.
        /// </summary>
        public event SubscribedEventHandler Subscribed;

        private static string Version { get; } = typeof(Pusher).GetTypeInfo().Assembly.GetName().Version.ToString(3);

        private readonly string _applicationKey;

        private IConnection _connection;

        /// <summary>
        /// Gets the Socket ID
        /// </summary>
        public string SocketID => _connection?.SocketId;

        /// <summary>
        /// Gets the current connection state
        /// </summary>
        public ConnectionState State => _connection?.State ?? ConnectionState.Uninitialized;

        /// <summary>
        /// Gets the channels in use by the Client
        /// </summary>
        private ConcurrentDictionary<string, Channel> Channels { get; } = new ConcurrentDictionary<string, Channel>();

        private ConcurrentBag<Channel> Backlog { get; } = new ConcurrentBag<Channel>();

        /// <summary>
        /// Gets the Options in use by the Client
        /// </summary>
        internal PusherOptions Options { get; private set; }

        private SemaphoreSlim _connectLock = new SemaphoreSlim(1);
        private SemaphoreSlim _disconnectLock = new SemaphoreSlim(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="Pusher" /> class.
        /// </summary>
        /// <param name="applicationKey">The application key.</param>
        /// <param name="options">The options.</param>
        public Pusher(string applicationKey, PusherOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(applicationKey))
                throw new ArgumentException(ErrorConstants.ApplicationKeyNotSet, nameof(applicationKey));

            _applicationKey = applicationKey;

            Options = options ?? new PusherOptions();
            ((IPusher)this).PusherOptions = Options;
            SetEventEmitterErrorHandler(InvokeErrorHandler);
        }

        PusherOptions IPusher.PusherOptions { get; set; }

        void IPusher.ChangeConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                Task.Run(() =>
                {
                    if (Connected != null)
                    {
                        try
                        {
                            Connected.Invoke(this);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new ConnectedEventHandlerException(error));
                        }
                    }

                    SubscribeExistingChannels();
                    UnsubscribeBacklog();

                    if (ConnectionStateChanged != null)
                    {
                        try
                        {
                            ConnectionStateChanged.Invoke(this, state);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new ConnectionStateChangedEventHandlerException(state, error));
                        }
                    }
                });
            }
            else if (state == ConnectionState.Disconnected)
            {
                Task.Run(() =>
                {
                    MarkChannelsAsUnsubscribed();
                    if (Disconnected != null)
                    {
                        try
                        {
                            Disconnected.Invoke(this);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new DisconnectedEventHandlerException(error));
                        }
                    }

                    if (ConnectionStateChanged != null)
                    {
                        try
                        {
                            ConnectionStateChanged.Invoke(this, state);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new ConnectionStateChangedEventHandlerException(state, error));
                        }
                    }
                });

            }
            else if (ConnectionStateChanged != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        ConnectionStateChanged.Invoke(this, state);
                    }
                    catch (Exception error)
                    {
                        InvokeErrorHandler(new ConnectionStateChangedEventHandlerException(state, error));
                    }
                });
            }
        }

        void IPusher.ErrorOccured(PusherException pusherException)
        {
            RaiseError(pusherException);
        }

        IEventBinder IPusher.GetEventBinder(string eventBinderKey)
        {
            return GetEventBinder(eventBinderKey);
        }

        IEventBinder IPusher.GetChannelEventBinder(string eventBinderKey, string channelName)
        {
            IEventBinder result = null;
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                result = channel.GetEventBinder(eventBinderKey);
            }

            return result;
        }

        void IPusher.AddMember(string channelName, string member)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (channel is IPresenceChannelManagement presenceChannel)
                {
                    presenceChannel.AddMember(member);
                }
            }
        }

        void IPusher.RemoveMember(string channelName, string member)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (channel is IPresenceChannelManagement presenceChannel)
                {
                    presenceChannel.RemoveMember(member);
                }
            }
        }

        void IPusher.SubscriptionSuceeded(string channelName, string data)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                channel.SubscriptionSucceeded(data);
                if (Subscribed != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Subscribed.Invoke(this, channel);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new SubscribedEventHandlerException(channel, error, data));
                        }
                    });
                }

                if (channel._subscribeCompleted != null)
                {
                    channel._subscribeCompleted.Release();
                }
            }
        }

        void IPusher.SubscriptionFailed(string channelName, string data)
        {
            ChannelException error = new ChannelException($"Unexpected error subscribing to channel {channelName}", ErrorCodes.SubscriptionError, channelName, _connection.SocketId)
            {
                MessageData = data,
            };
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                error.Channel = channel;
                channel._subscriptionError = error;
                channel._subscribeCompleted?.Release();
            }

            RaiseError(error);
        }

        byte[] IPusher.GetSharedSecret(string channelName)
        {
            byte[] result = null;
            Channel channel = GetChannel(channelName);
            if (channel != null && channel is PrivateChannel privateChannel)
            {
                result = privateChannel.SharedSecret;
            }

            return result;
        }

        /// <summary>
        /// Connect to the Pusher Server.
        /// </summary>
        /// <exception cref="OperationTimeoutException">
        /// If the client times out waiting for confirmation of the connect. The timeout is defind in <see cref="PusherOptions"/>.
        /// </exception>
        public async Task ConnectAsync()
        {
            if (_connection != null && _connection.IsConnected)
            {
                return;
            }

            try
            {
                // Prevent multiple concurrent connections
                TimeSpan timeoutPeriod = Options.ClientTimeout;
                if (!await _connectLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, Constants.CONNECTION_ESTABLISHED);
                }

                try
                {
                    // Ensure we only ever attempt to connect once
                    if (_connection != null && _connection.IsConnected)
                    {
                        return;
                    }

                    var url = ConstructUrl();

                    _connection = new Connection(this, url);
                    _disconnectLock = new SemaphoreSlim(1);
                    await _connection.ConnectAsync().ConfigureAwait(false);
                }
                finally
                {
                    _connectLock.Release();
                }
            }
            catch (Exception e)
            {
                HandleOperationException(ErrorCodes.ConnectError, $"{nameof(Pusher)}.{nameof(Pusher.ConnectAsync)}", e);
                throw;
            }
        }

        private string ConstructUrl()
        {
            var scheme = Options.Encrypted ? Constants.SECURE_SCHEMA : Constants.INSECURE_SCHEMA;

            return $"{scheme}{Options.Host}/app/{_applicationKey}?protocol=5&client=pusher-dotnet-client&version={Version}";
        }

        /// <summary>
        /// Disconnect from the Pusher Server.
        /// </summary>
        /// <exception cref="OperationTimeoutException">
        /// If the client times out waiting for confirmation of the disconnect. The timeout is defind in <see cref="PusherOptions"/>.
        /// </exception>
        public async Task DisconnectAsync()
        {
            if (_connection == null || State == ConnectionState.Disconnected)
            {
                return;
            }

            try
            {
                TimeSpan timeoutPeriod = Options.ClientTimeout;
                if (!await _disconnectLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{nameof(Pusher)}.{nameof(Pusher.DisconnectAsync)}");
                }

                try
                {
                    if (_connection != null)
                    {
                        if (State != ConnectionState.Disconnected)
                        {
                            _connectLock = new SemaphoreSlim(1);
                            MarkChannelsAsUnsubscribed();
                            await _connection.DisconnectAsync().ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    _disconnectLock.Release();
                }
            }
            catch (Exception e)
            {
                HandleOperationException(ErrorCodes.DisconnectError, $"{nameof(Pusher)}.{nameof(Pusher.DisconnectAsync)}", e);
                throw;
            }
        }

        /// <summary>
        /// Subscribes to a channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to subsribe to.</param>
        /// <param name="subscribedEventHandler">An optional <see cref="SubscriptionEventHandler"/>. Alternatively, use <c>Pusher.Subscribed</c>.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">Only for private or presence channels if authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">Only for private or presence channels if an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <exception cref="ChannelException">If the client receives an error (pusher:error) from the Pusher cluster while trying to subscribe.</exception>
        /// <exception cref="OperationTimeoutException">
        /// If the client times out waiting for the Pusher server to confirm the subscription. The timeout is defind in <see cref="PusherOptions"/>.
        /// </exception>
        /// <returns>The channel identified by <paramref name="channelName"/>.</returns>
        /// <remarks>
        /// If Pusher is connected when calling this method, the channel will be subscribed when this method returns;
        /// that is <c>channel.IsSubscribed == true</c>.
        /// If Pusher is not connected when calling this method, <c>channel.IsSubscribed == false</c> and 
        /// the channel will only be subscribed after calling <c>Pusher.ConnectAsync</c>.
        /// </remarks>
        public async Task<Channel> SubscribeAsync(string channelName, SubscriptionEventHandler subscribedEventHandler = null)
        {
            Guard.ChannelName(channelName);

            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (channel.IsSubscribed)
                {
                    return channel;
                }
            }

            return await SubscribeAsync(channelName, channel, subscribedEventHandler).ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribes to a typed member info presence channel.
        /// </summary>
        /// <typeparam name="MemberT">The type used to deserialize channel member info.</typeparam>
        /// <param name="channelName">The name of the channel to subsribe to.</param>
        /// <param name="subscribedEventHandler">An optional <see cref="SubscriptionEventHandler"/>. Alternatively, use <c>Pusher.Subscribed</c>.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="channelName"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ChannelUnauthorizedException">If authorization fails.</exception>
        /// <exception cref="ChannelAuthorizationFailureException">f an HTTP call to the authorization URL fails; that is, the HTTP status code is outside of the range 200-299.</exception>
        /// <exception cref="ChannelException">If the client receives an error (pusher:error) from the Pusher cluster while trying to subscribe.</exception>
        /// <exception cref="OperationTimeoutException">
        /// If the client times out waiting for the Pusher server to confirm the subscription. The timeout is defind in the <see cref="PusherOptions"/>.
        /// </exception>
        /// <returns>A GenericPresenceChannel<MemberT> channel identified by <paramref name="channelName"/>.</returns>
        /// <remarks>
        /// If Pusher is connected when calling this method, the channel will be subscribed when this method returns;
        /// that is <c>channel.IsSubscribed == true</c>.
        /// If Pusher is not connected when calling this method, <c>channel.IsSubscribed == false</c> and 
        /// the channel will only be subscribed after calling <c>Pusher.ConnectAsync</c>.
        /// </remarks>
        public async Task<GenericPresenceChannel<MemberT>> SubscribePresenceAsync<MemberT>(
            string channelName,
            SubscriptionEventHandler subscribedEventHandler = null)
        {
            Guard.ChannelName(channelName);

            var channelType = Channel.GetChannelType(channelName);
            if (channelType != ChannelTypes.Presence)
            {
                throw new ArgumentException($"The channel name '{channelName}' is not that of a presence channel. Expecting the name to start with '{Constants.PRESENCE_CHANNEL}'.", nameof(channelName));
            }

            GenericPresenceChannel<MemberT> result;
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (!(channel is GenericPresenceChannel<MemberT> presenceChannel))
                {
                    string errorMsg;
                    if (channel is PresenceChannel)
                    {
                        errorMsg = $"The presence channel '{channelName}' has already been created as a {nameof(PresenceChannel)} : {nameof(PresenceChannel)}<dynamic>.";
                    }
                    else
                    {
                        errorMsg = $"The presence channel '{channelName}' has already been created but with a different type: {channel.GetType()}";
                    }

                    throw new ChannelException(errorMsg, ErrorCodes.PresenceChannelAlreadyDefined, channelName, SocketID);
                }

                if (presenceChannel.IsSubscribed)
                {
                    result = presenceChannel;
                }
                else
                {
                    result = (await SubscribeAsync(channelName, presenceChannel, subscribedEventHandler).ConfigureAwait(false)) as GenericPresenceChannel<MemberT>;
                }
            }
            else
            {
                AuthEndpointCheck(channelName);
                result = new GenericPresenceChannel<MemberT>(channelName, this);
                if (Channels.TryAdd(channelName, result))
                {
                    result = (await SubscribeAsync(channelName, result, subscribedEventHandler).ConfigureAwait(false)) as GenericPresenceChannel<MemberT>;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to get.</param>
        /// <returns>The <see cref="Channel"/> if it exists; otherwise <c>null</c>.</returns>
        public Channel GetChannel(string channelName)
        {
            Channel result = null;
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                result = channel;
            }

            return result;
        }

        /// <summary>
        /// Get all current channels.
        /// </summary>
        /// <returns>A list of the current channels.</returns>
        public IList<Channel> GetAllChannels()
        {
            List<Channel> result = new List<Channel>(Channels.Count);
            foreach (Channel channel in Channels.Values)
            {
                result.Add(channel);
            }

            return result;
        }

        /// <summary>
        /// Removes a channel subscription.
        /// </summary>
        /// <param name="channelName">The name of the channel to unsubscribe.</param>
        /// <returns>An awaitable task to use with async operations.</returns>
        public async Task UnsubscribeAsync(string channelName)
        {
            Guard.ChannelName(channelName);
            if (Channels.TryRemove(channelName, out Channel channel))
            {
                channel.UnbindAll();
                if (channel.IsServerSubscribed && !_connection.IsConnected)
                {
                    // No connection to send a pusher:unsubscribe message so add to the backlog until connected again.
                    // If we do not do this we could still receive channel events later even though we unsubscribed.
                    Backlog.Add(channel);
                }
                else
                {
                    await SendUnsubscribeAsync(channel).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Removes all channel subscriptions.
        /// </summary>
        /// <returns>An awaitable task to use with async operations.</returns>
        public async Task UnsubscribeAllAsync()
        {
            IList<Channel> channels = GetAllChannels();
            if (channels.Count > 0)
            {
                // Unsubscribe in the following order - Presence, Private, Public
                var sorted = channels.Select(c => c).OrderByDescending(c => c.ChannelType);
                foreach (var channel in sorted)
                {
                    try
                    {
                        await UnsubscribeAsync(channel.Name).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        HandleSubscriptionException("Unsubscribe", channel, exception);
                    }
                }
            }
        }

        private async Task<Channel> SubscribeAsync(string channelName, Channel channel, SubscriptionEventHandler subscribedEventHandler = null)
        {
            if (channel == null)
            {
                channel = CreateChannel(channelName);
            }

            if (subscribedEventHandler != null)
            {
                channel.Subscribed -= subscribedEventHandler;
                channel.Subscribed += subscribedEventHandler;
            }

            if (State == ConnectionState.Connected)
            {
                try
                {

                    TimeSpan timeoutPeriod = Options.ClientTimeout;
                    if (!await channel._subscribeLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                    {
                        throw new OperationTimeoutException(timeoutPeriod, $"{Constants.CHANNEL_SUBSCRIPTION_SUCCEEDED} on {channelName}");
                    }

                    if (!channel.IsSubscribed)
                    {
                        if (Channels.ContainsKey(channelName))
                        {
                            await SubscribeChannelAsync(channel).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception error)
                {
                    HandleSubscribeChannelError(channel, error);
                    throw;
                }
                finally
                {
                    channel._subscribeLock.Release();
                }
            }

            return channel;
        }

        private async Task SubscribeChannelAsync(Channel channel)
        {
            string channelName = channel.Name;
            ErrorEventHandler errorHandler = null;
            channel._subscribeCompleted = new SemaphoreSlim(0, 1);
            try
            {
                void SubscribeErrorHandler(object sender, PusherException error)
                {
                    if ((int)error.PusherCode < 5000)
                    {
                        // If we receive an error from the Pusher cluster then we need to raise a channel subscription error
                        channel._subscriptionError = new ChannelException(ErrorCodes.SubscriptionError, channel.Name, _connection.SocketId, error);
                        if (channel._subscribeCompleted != null)
                        {
                            channel._subscribeCompleted.Release();
                        }
                    }
                }

                channel._subscriptionError = null;
                errorHandler = SubscribeErrorHandler;
                Error += errorHandler;
                string message;
                if (channel.ChannelType != ChannelTypes.Public)
                {
                    string jsonAuth;
                    if (Options.Authorizer is IAuthorizerAsync asyncAuthorizer)
                    {
                        if (!asyncAuthorizer.Timeout.HasValue)
                        {
                            // Use a timeout interval that is less than the outer subscription timeout.
                            asyncAuthorizer.Timeout = Options.InnerClientTimeout;
                        }

                        jsonAuth = await asyncAuthorizer.AuthorizeAsync(channelName, _connection.SocketId).ConfigureAwait(false);
                    }
                    else
                    {
                        jsonAuth = Options.Authorizer.Authorize(channelName, _connection.SocketId);
                    }

                    message = CreateAuthorizedChannelSubscribeMessage(channel, jsonAuth);
                }
                else
                {
                    message = CreateChannelSubscribeMessage(channelName);
                }

                await _connection.SendAsync(message).ConfigureAwait(false);

                TimeSpan timeoutPeriod = Options.InnerClientTimeout;
                if (!await channel._subscribeCompleted.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.CHANNEL_SUBSCRIPTION_SUCCEEDED} on {channelName}");
                }

                if (channel._subscriptionError != null)
                {
                    throw channel._subscriptionError;
                }
            }
            finally
            {
                if (errorHandler != null)
                {
                    Error -= errorHandler;
                }

                channel._subscribeCompleted.Dispose();
                channel._subscribeCompleted = null;
            }
        }

        private void HandleSubscribeChannelError(Channel channel, Exception exception)
        {
            channel.IsSubscribed = false;
            if (exception is PusherException error)
            {
                if (error is IChannelException channelException)
                {
                    channelException.ChannelName = channel.Name;
                    channelException.Channel = channel;
                    channelException.SocketID = SocketID;
                    Channels.TryRemove(channel.Name, out _);
                }
            }
            else
            {
                error = new OperationException(ErrorCodes.SubscriptionError, $"Subscribe to {channel.Name}", exception);
            }

            RaiseError(error);
        }

        private string CreateAuthorizedChannelSubscribeMessage(Channel channel, string jsonAuth)
        {
            string channelName = channel.Name;
            string auth = null;
            string channelData = null;
            JObject jObject = JObject.Parse(jsonAuth);
            JToken jToken = jObject.SelectToken("auth");
            if (jToken != null)
            {
                if (jToken.Type == JTokenType.String)
                {
                    auth = jToken.Value<string>();
                }
            }

            jToken = jObject.SelectToken("channel_data");
            if (jToken != null && jToken.Type == JTokenType.String)
            {
                channelData = jToken.Value<string>();
            }

            jToken = jObject.SelectToken("shared_secret");
            if (jToken != null &&
                jToken.Type == JTokenType.String &&
                channel.ChannelType == ChannelTypes.PrivateEncrypted &&
                channel is PrivateChannel privateChannel)
            {
                string secret = jToken.Value<string>();
                privateChannel.SharedSecret = Convert.FromBase64String(secret);
            }

            PusherChannelSubscriptionData data = new PusherAuthorizedChannelSubscriptionData(channelName, auth, channelData);
            return DefaultSerializer.Default.Serialize(new PusherChannelSubscribeEvent(data));
        }

        private string CreateChannelSubscribeMessage(string channelName)
        {
            PusherChannelSubscriptionData data = new PusherChannelSubscriptionData(channelName);
            return DefaultSerializer.Default.Serialize(new PusherChannelSubscribeEvent(data));
        }

        private Channel CreateChannel(string channelName)
        {
            ChannelTypes type = Channel.GetChannelType(channelName);
            Channel result;
            switch (type)
            {
                case ChannelTypes.Private:
                case ChannelTypes.PrivateEncrypted:
                    AuthEndpointCheck(channelName);
                    result = new PrivateChannel(channelName, this);
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck(channelName);
                    result = new PresenceChannel(channelName, this);
                    break;
                default:
                    result = new Channel(channelName, this);
                    break;
            }

            if (!Channels.TryAdd(channelName, result))
            {
                result = Channels[channelName];
            }

            return result;
        }

        private void AuthEndpointCheck(string channelName)
        {
            if (Options.Authorizer == null)
            {
                string errorMsg = $"An Authorizer needs to be provided when subscribing to the private or presence channel '{channelName}'.";
                ChannelException pusherException = new ChannelException(errorMsg, ErrorCodes.ChannelAuthorizerNotSet, channelName: channelName, socketId: SocketID);
                RaiseError(pusherException);
                throw pusherException;
            }
        }

        private void ValidateTriggerInput(string channelName, string eventName)
        {
            if (Channel.GetChannelType(channelName) == ChannelTypes.Public)
            {
                string errorMsg = $"Failed to trigger event '{eventName}'. Client events are only supported on private (non-encrypted) and presence channels.";
                throw new TriggerEventException(errorMsg, ErrorCodes.TriggerEventPublicChannelError);
            }

            if (Channel.GetChannelType(channelName) == ChannelTypes.PrivateEncrypted)
            {
                string errorMsg = $"Failed to trigger event '{eventName}'. Client events are not supported on private encrypted channels.";
                throw new TriggerEventException(errorMsg, ErrorCodes.TriggerEventPrivateEncryptedChannelError);
            }

            string token = "client-";
            if (eventName.IndexOf(token, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string errorMsg = $"Failed to trigger event '{eventName}'. Client events must start with '{token}'.";
                throw new TriggerEventException(errorMsg, ErrorCodes.TriggerEventNameInvalidError);
            }

            if (State != ConnectionState.Connected)
            {
                string errorMsg = $"Failed to trigger event '{eventName}'. Client needs to be connected. Current connection state is {State}.";
                throw new TriggerEventException(errorMsg, ErrorCodes.TriggerEventNotConnectedError);
            }

            bool subscribed = false;
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                subscribed = channel.IsSubscribed;
            }

            if (!subscribed)
            {
                string errorMsg = $"Failed to trigger event '{eventName}'. Channel '{channelName}' needs to be subscribed.";
                throw new TriggerEventException(errorMsg, ErrorCodes.TriggerEventNotSubscribedError);
            }
        }

        async Task ITriggerChannels.TriggerAsync(string channelName, string eventName, object obj)
        {
            ValidateTriggerInput(channelName, eventName);
            string message = DefaultSerializer.Default.Serialize(new PusherChannelEvent(eventName, obj, channelName));
            await _connection.SendAsync(message).ConfigureAwait(false);
        }

        async Task ITriggerChannels.ChannelUnsubscribeAsync(string channelName)
        {
            await UnsubscribeAsync(channelName);
        }

        void ITriggerChannels.RaiseChannelError(PusherException error)
        {
            RaiseError(error, runAsNewTask: false);
        }

        private void RaiseError(PusherException error, bool runAsNewTask = true)
        {
            if (Error != null)
            {
                if (!error.EmittedToErrorHandler)
                {
                    if (runAsNewTask)
                    {
                        Task.Run(() =>
                        {
                            InvokeErrorHandler(error);
                        });
                    }
                    else
                    {
                        InvokeErrorHandler(error);
                    }
                }
            }
            else
            {
                error.EmittedToErrorHandler = true;
            }
        }

        private void InvokeErrorHandler(PusherException error)
        {
            try
            {
                if (Error != null)
                {
                    if (!error.EmittedToErrorHandler)
                    {
                        try
                        {
                            Error.Invoke(this, error);
                        }
                        catch (Exception e)
                        {
                            if (Options.TraceLogger != null)
                            {
                                Options.TraceLogger.TraceError($"Error caught invoking delegate Pusher.Error:{Environment.NewLine}{e}");
                            }
                        }
                    }
                }
            }
            finally
            {
                error.EmittedToErrorHandler = true;
            }
        }

        private async Task SendUnsubscribeAsync(Channel channel)
        {
            if (_connection != null && _connection.IsConnected)
            {
                if (channel.IsSubscribed)
                {
                    try
                    {
                        if (channel.IsSubscribed)
                        {
                            await _connection.SendAsync(DefaultSerializer.Default.Serialize(new PusherChannelUnsubscribeEvent(channel.Name))).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        HandleOperationException(ErrorCodes.SubscriptionError, $"{nameof(Pusher)}.{nameof(Pusher.UnsubscribeAsync)}", e);
                        throw;
                    }
                    finally
                    {
                        channel.IsSubscribed = false;
                    }
                }
            }
            else
            {
                channel.IsSubscribed = false;
            }
        }

        private void HandleOperationException(ErrorCodes code, string operation, Exception exception)
        {
            if (!(exception is PusherException error))
            {
                error = new OperationException(code, operation, exception);
            }

            RaiseError(error);
        }

        private void HandleSubscriptionException(string action, Channel channel, Exception exception)
        {
            Exception error = exception;
            if (exception is AggregateException aggregateException)
            {
                error = aggregateException.InnerException;
            }

            if (!(error is PusherException))
            {
                error = new ChannelException(
                    $"{action} failed for channel '{channel.Name}':{Environment.NewLine}{exception.Message}",
                    ErrorCodes.SubscriptionError,
                    channel.Name,
                    SocketID,
                    error)
                {
                    Channel = channel,
                };
            }

            RaiseError(error as PusherException);
        }

        private void UnsubscribeBacklog()
        {
            while (!Backlog.IsEmpty)
            {
                if (Backlog.TryTake(out Channel channel))
                {
                    if (!Channels.ContainsKey(channel.Name))
                    {
                        try
                        {
                            channel.IsSubscribed = true;
                            Task.WaitAll(Task.Run(() => SendUnsubscribeAsync(channel)));
                        }
                        catch (Exception exception)
                        {
                            HandleSubscriptionException("Unsubscribe", channel, exception);
                        }
                    }
                }
            }
        }

        private void MarkChannelsAsUnsubscribed()
        {
            List<Channel> candidates = new List<Channel>(Channels.Count);
            bool connected = _connection.IsConnected;
            foreach (Channel channel in Channels.Values)
            {
                if (channel.IsSubscribed)
                {
                    if (connected)
                    {
                        candidates.Add(channel);
                    }
                    else
                    {
                        channel.IsSubscribed = false;
                    }
                }
            }

            if (candidates.Count > 0)
            {
                // Unsubscribe in the following order - Presence, Private, Public
                var sorted = candidates.Select(c => c).OrderByDescending(c => c.ChannelType);
                foreach (Channel channel in sorted)
                {
                    try
                    {
                        channel._subscribeLock = new SemaphoreSlim(1);
                        Task.WaitAll(Task.Run(() => SendUnsubscribeAsync(channel)));
                    }
                    catch (Exception exception)
                    {
                        HandleSubscriptionException("Unsubscribe", channel, exception);
                    }
                }
            }
        }

        private void SubscribeExistingChannels()
        {
            IList<Channel> channels = GetAllChannels();
            if (channels.Count > 0)
            {
                // Subscribe in the following order - Public, Private, Presence
                var sorted = channels.Select(c => c).OrderBy(c => c.ChannelType);
                foreach (var channel in sorted)
                {
                    try
                    {
                        Task.WaitAll(Task.Run(() => SubscribeAsync(channel.Name, channel)));
                    }
                    catch (AggregateException aggregateException)
                    {
                        HandleSubscriptionException("Subscribe", channel, aggregateException);
                    }
                }
            }
        }
    }
}
