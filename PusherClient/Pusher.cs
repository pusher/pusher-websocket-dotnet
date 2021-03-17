using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PusherClient
{
    /* TODO: Write tests
     * - Websocket disconnect
        - Connection lost, not cleanly closed
        - MustConnectOverSSL = 4000,
        - App does not exist
        - App disabled
        - Over connection limit
        - Path not found
        - Client over rate limie
        - Conditions for client event triggering
     */
    // TODO: Implement connection fallback strategy

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

        /// <summary>
        /// The TraceSource instance to be used for logging
        /// </summary>
        public static TraceSource Trace = new TraceSource(nameof(Pusher));

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

        private readonly SemaphoreSlim _mutexLock = new SemaphoreSlim(1);

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
            ((IPusher)this).IsTracingEnabled = Options.IsTracingEnabled;
        }

        bool IPusher.IsTracingEnabled { get; set; }

        void IPusher.ChangeConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                UnsubscribeBacklog();
                if (Connected != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Connected.Invoke(this);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new ConnectedEventHandlerException(error));
                        }
                    });
                }

                SubscribeExistingChannels();
            }
            else if (state == ConnectionState.Disconnected)
            {
                MarkChannelsAsUnsubscribed();
                if (Disconnected != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Disconnected.Invoke(this);
                        }
                        catch (Exception error)
                        {
                            InvokeErrorHandler(new DisconnectedEventHandlerException(error));
                        }
                    });
                }
            }

            if (ConnectionStateChanged != null)
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

        void IPusher.EmitPusherEvent(string eventName, PusherEvent data)
        {
            EmitEvent(eventName, data);
        }

        void IPusher.EmitChannelEvent(string channelName, string eventName, PusherEvent data)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                channel.EmitEvent(eventName, data);
            }
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

        /// <summary>
        /// Connect to the Pusher Server.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_connection != null && _connection.IsConnected)
            {
                return;
            }

            // Prevent multiple concurrent connections
            await _mutexLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Ensure we only ever attempt to connect once
                if (_connection != null && _connection.IsConnected)
                {
                    return;
                }

                var url = ConstructUrl();

                _connection = new Connection(this, url);
                await _connection.ConnectAsync().ConfigureAwait(false);
            }
            finally
            {
                _mutexLock.Release();
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
        public async Task DisconnectAsync()
        {
            if (_connection == null || State == ConnectionState.Disconnected)
            {
                return;
            }

            await _mutexLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_connection != null)
                {
                    if (State != ConnectionState.Disconnected)
                    {
                        MarkChannelsAsUnsubscribed();
                        await _connection.DisconnectAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _mutexLock.Release();
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
                throw new ArgumentException($"The channel name '{channelName}' is not that of a presence channel.", nameof(channelName));
            }

            GenericPresenceChannel<MemberT> result;
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (!(channel is GenericPresenceChannel<MemberT> presenceChannel))
                {
                    if (channel is PresenceChannel)
                    {
                        throw new InvalidOperationException("This presence channel has already been created without specifying the member info type.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"The presence channel has already been created but with a different type: {channel.GetType()}");
                    }
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
                AuthEndpointCheck();
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

        public async Task UnsubscribeAsync(string channelName)
        {
            Guard.ChannelName(channelName);
            if (Channels.TryRemove(channelName, out Channel channel))
            {
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
                await channel._subscribeLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!channel.IsSubscribed)
                    {
                        if (Channels.ContainsKey(channelName))
                        {
                            await SubscribeChannelAsync(channel).ConfigureAwait(false);
                        }
                    }
                }
                catch (PusherException pusherError)
                {
                    HandleSubscribeChannelError(channel, pusherError);
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
                channel._subscriptionError = null;
                errorHandler = (sender, error) =>
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
                };
                Error += errorHandler;
                string message;
                if (channel.ChannelType != ChannelTypes.Public)
                {
                    string jsonAuth;
                    if (Options.Authorizer is IAuthorizerAsync asyncAuthorizer)
                    {
                        jsonAuth = await asyncAuthorizer.AuthorizeAsync(channelName, _connection.SocketId).ConfigureAwait(false);
                    }
                    else
                    {
                        jsonAuth = Options.Authorizer.Authorize(channelName, _connection.SocketId);
                    }

                    message = CreateAuthorizedChannelSubscribeMessage(channelName, jsonAuth);
                }
                else
                {
                    message = CreateChannelSubscribeMessage(channelName);
                }

                await _connection.SendAsync(message).ConfigureAwait(false);

                await channel._subscribeCompleted.WaitAsync().ConfigureAwait(false);
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

        private void HandleSubscribeChannelError(Channel channel, PusherException pusherError)
        {
            channel.IsSubscribed = false;
            if (pusherError is ChannelException channelException)
            {
                channelException.Channel = channel;
                Channels.TryRemove(channel.Name, out _);
            }

            RaiseError(pusherError);
        }

        private string CreateAuthorizedChannelSubscribeMessage(string channelName, string jsonAuth)
        {
            var template = new { auth = string.Empty, channel_data = string.Empty };
            dynamic messageObj = JsonConvert.DeserializeAnonymousType(jsonAuth, template);
            return JsonConvert.SerializeObject(new
            {
                @event = Constants.CHANNEL_SUBSCRIBE,
                data = new
                {
                    channel = channelName,
                    messageObj.auth,
                    messageObj.channel_data
                }
            });
        }

        private string CreateChannelSubscribeMessage(string channelName)
        {
            return JsonConvert.SerializeObject(new
            {
                @event = Constants.CHANNEL_SUBSCRIBE,
                data = new { channel = channelName }
            });
        }

        private Channel CreateChannel(string channelName)
        {
            ChannelTypes type = Channel.GetChannelType(channelName);
            Channel result;
            switch (type)
            {
                case ChannelTypes.Private:
                    AuthEndpointCheck();
                    result = new PrivateChannel(channelName, this);
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck();
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

        private void AuthEndpointCheck()
        {
            if (Options.Authorizer == null)
            {
                var pusherException = new PusherException("An Authorizer needs to be provided when subscribing to a private or presence channel.", ErrorCodes.ChannelAuthorizerNotSet);
                RaiseError(pusherException);
                throw pusherException;
            }
        }

        async Task ITriggerChannels.TriggerAsync(string channelName, string eventName, object obj)
        {
            string message = JsonConvert.SerializeObject(new
            {
                @event = eventName,
                channel = channelName,
                data = obj,
            });
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
            if (Error != null && !error.EmittedToErrorHandler)
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
                            if (Options.IsTracingEnabled)
                            {
                                Trace.TraceInformation($"Error caught invoking delegate Pusher.Error:{Environment.NewLine}{e}");
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
            if (channel.IsSubscribed && _connection.IsConnected)
            {
                await channel._subscribeLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (channel.IsSubscribed)
                    {
                        channel.IsSubscribed = false;
                        await _connection.SendAsync(JsonConvert.SerializeObject(new
                        {
                            @event = Constants.CHANNEL_UNSUBSCRIBE,
                            data = new { channel = channel.Name },
                        })).ConfigureAwait(false);
                    }
                }
                finally
                {
                    channel._subscribeLock.Release();
                }
            }
            else
            {
                channel.IsSubscribed = false;
            }
        }

        private void HandleSubscriptionException(string action, Channel channel, Exception exception)
        {
            Exception innerException = exception;
            if (exception is AggregateException aggregateException)
            {
                innerException = aggregateException.InnerException;
            }

            if (innerException is PusherException pusherException)
            {
                RaiseError(pusherException);
            }
            else
            {
                ChannelException channelException = new ChannelException(
                    $"{action} failed for channel '{channel.Name}':{Environment.NewLine}{exception.Message}",
                    ErrorCodes.SubscriptionError,
                    channel.Name,
                    SocketID,
                    innerException)
                {
                    Channel = channel,
                };
                RaiseError(channelException);
            }
        }

        private void UnsubscribeBacklog()
        {
            Task.Run(() =>
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
                            catch (AggregateException aggregateException)
                            {
                                HandleSubscriptionException("Unsubscribe", channel, aggregateException);
                            }
                        }
                    }
                }
            });
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
                        Task.WaitAll(Task.Run(() => SendUnsubscribeAsync(channel)));
                    }
                    catch (AggregateException aggregateException)
                    {
                        HandleSubscriptionException("Unsubscribe", channel, aggregateException);
                    }
                }
            }
        }

        private void SubscribeExistingChannels()
        {
            IList<Channel> channels = GetAllChannels();
            if (channels.Count == 0)
            {
                return;
            }

            // Task needs to run asynchronously otherwise we get task deadlock
            Task.Run(() =>
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
            });
        }
    }
}
