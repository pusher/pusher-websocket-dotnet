using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly PusherOptions _options;

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

        /// <summary>
        /// Gets the Options in use by the Client
        /// </summary>
        internal PusherOptions Options => _options;

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

            _options = options ?? new PusherOptions();
            ((IPusher)this).IsTracingEnabled = _options.IsTracingEnabled;
        }

        bool IPusher.IsTracingEnabled { get; set; }

        void IPusher.ChangeConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                SubscribeExistingChannels();

                try
                {
                    Connected?.Invoke(this);
                }
                catch (Exception error)
                {
                    RaiseError(new ConnectedDelegateException(error));
                }
            }
            else if (state == ConnectionState.Disconnected)
            {
                MarkChannelsAsUnsubscribed();

                try
                {
                    Disconnected?.Invoke(this);
                }
                catch (Exception error)
                {
                    RaiseError(new DisconnectedDelegateException(error));
                }
            }

            try
            {
                ConnectionStateChanged?.Invoke(this, state);
            }
            catch (Exception error)
            {
                RaiseError(new ConnectionStateChangedDelegateException(state, error));
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
            if (Channels.TryGetValue(channelName, out Channel channel) && channel is PresenceChannel presenceChannel)
            {
                presenceChannel.AddMember(member);
            }
        }

        void IPusher.RemoveMember(string channelName, string member)
        {
            if (Channels.TryGetValue(channelName, out Channel channel) && channel is PresenceChannel presenceChannel)
            {
                presenceChannel.RemoveMember(member);
            }
        }

        void IPusher.SubscriptionSuceeded(string channelName, string data)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                channel.SubscriptionSucceeded(data);
                try
                {
                    Subscribed?.Invoke(this, channelName);
                }
                catch (Exception error)
                {
                    RaiseError(new SubscribedDelegateException(channelName, error, data));
                }

                channel._subscribeCompleted?.Release();
            }
        }

        void IPusher.SubscriptionFailed(string channelName, string data)
        {
            SubscriptionException error = new SubscriptionException(channelName, data);
            RaiseError(error);
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                channel._subscriptionError = error;
                channel._subscribeCompleted?.Release();
            }
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
            var scheme = _options.Encrypted ? Constants.SECURE_SCHEMA : Constants.INSECURE_SCHEMA;

            return $"{scheme}{_options.Host}/app/{_applicationKey}?protocol=5&client=pusher-dotnet-client&version={Version}";
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
                if (_connection != null && State != ConnectionState.Disconnected)
                {
                    MarkChannelsAsUnsubscribed();
                    await _connection.DisconnectAsync().ConfigureAwait(false);
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
        /// <exception cref="System.Net.Http.HttpRequestException">Only for private or presence channels if an HTTP call to the authorization URL fails.</exception>
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
        /// <exception cref="System.Net.Http.HttpRequestException">If an HTTP call to the authorization URL fails.</exception>
        /// <returns>A GenericPresenceChannel<MemberT> channel identified by <paramref name="channelName"/>.</returns>
        /// <remarks>
        /// If Pusher is connected when calling this method, the channel will be subscribed when this method returns;
        /// that is <c>channel.IsSubscribed == true</c>.
        /// If Pusher is not connected when calling this method, <c>channel.IsSubscribed == false</c> and 
        /// the channel will only be subscribed after calling <c>Pusher.ConnectAsync</c>.
        /// </remarks>
        public async Task<GenericPresenceChannel<MemberT>> SubscribePresenceAsync<MemberT>(string channelName, SubscriptionEventHandler subscribedEventHandler = null)
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
                result = new GenericPresenceChannel<MemberT>(channelName, this, Options);
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
                bool raiseError = true;
                await channel._subscribeLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!channel.IsSubscribed && Channels.ContainsKey(channelName))
                    {
                        channel._subscribeCompleted = new SemaphoreSlim(0, 1);
                        try
                        {
                            string message;
                            if (channel.ChannelType == ChannelTypes.Presence || channel.ChannelType == ChannelTypes.Private)
                            {
                                try
                                {
                                    string jsonAuth;
                                    if (_options.Authorizer is IAuthorizerAsync asyncAuthorizer)
                                    {
                                        jsonAuth = await asyncAuthorizer.AuthorizeAsync(channelName, _connection.SocketId).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        jsonAuth = _options.Authorizer.Authorize(channelName, _connection.SocketId);
                                    }

                                    message = CreateAuthorizedChannelSubscribeMessage(channelName, jsonAuth);
                                }
                                catch (ChannelUnauthorizedException unauthorizedEx)
                                {
                                    if (Channels.TryRemove(channelName, out Channel unauthorizedChannel))
                                    {
                                        unauthorizedChannel.IsSubscribed = false;
                                        unauthorizedEx.Channel = unauthorizedChannel;
                                    }

                                    throw unauthorizedEx;
                                }
                            }
                            else
                            {
                                message = CreateChannelSubscribeMessage(channelName);
                            }

                            await _connection.SendAsync(message).ConfigureAwait(false);

                            await channel._subscribeCompleted.WaitAsync().ConfigureAwait(false);
                            if (channel._subscriptionError != null)
                            {
                                // Error already raised in IPusher.SubscriptionFailed
                                raiseError = false;
                                throw channel._subscriptionError;
                            }
                        }
                        finally
                        {
                            channel._subscribeCompleted.Dispose();
                            channel._subscribeCompleted = null;
                        }
                    }
                }
                catch (PusherException pusherError)
                {
                    if (raiseError) RaiseError(pusherError);
                    throw;
                }
                finally
                {
                    channel._subscribeLock.Release();
                }
            }

            return channel;
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
                    result = new PrivateChannel(channelName, this, Options);
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck();
                    result = new PresenceChannel(channelName, this, Options);
                    break;
                default:
                    result = new Channel(channelName, this, Options);
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
            if (_options.Authorizer == null)
            {
                var pusherException = new PusherException("An Authorizer needs to be provided when subscribing to a private or presence channel.", ErrorCodes.ChannelAuthorizerNotSet);
                RaiseError(pusherException);
                throw pusherException;
            }
        }

        async Task ITriggerChannels.Trigger(string channelName, string eventName, object obj)
        {
            string message = JsonConvert.SerializeObject(new
            {
                @event = eventName,
                channel = channelName,
                data = obj,
            });
            await _connection.SendAsync(message).ConfigureAwait(false);
        }

        async Task ITriggerChannels.SendUnsubscribe(Channel channel)
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

        void ITriggerChannels.RaiseSubscribedError(PusherException error)
        {
            RaiseError(error);
        }

        private void RaiseError(PusherException error)
        {
            if (Error != null)
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

        private void MarkChannelsAsUnsubscribed()
        {
            foreach (var channel in Channels.Values)
            {
                Task.Run(() => ((ITriggerChannels)this).SendUnsubscribe(channel));
            }
        }

        private void SubscribeExistingChannels()
        {
            foreach (var channel in Channels)
            {
                Task.Run(() => SubscribeAsync(channel.Key, channel.Value));
            }
        }
    }
}
