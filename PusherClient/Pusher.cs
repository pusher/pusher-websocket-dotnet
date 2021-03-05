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
        private readonly List<string> _pendingChannelSubscriptions = new List<string>();

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
            if (Channels.ContainsKey(channelName))
            {
                Channels[channelName].EmitEvent(eventName, data);
            }
        }

        void IPusher.AddMember(string channelName, string member)
        {
            if (Channels.Keys.Contains(channelName) && Channels[channelName] is PresenceChannel channel)
            {
                channel.AddMember(member);
            }
        }

        void IPusher.RemoveMember(string channelName, string member)
        {
            if (Channels.Keys.Contains(channelName) && Channels[channelName] is PresenceChannel channel)
            {
                channel.RemoveMember(member);
            }
        }

        void IPusher.SubscriptionSuceeded(string channelName, string data)
        {
            if (_pendingChannelSubscriptions.Contains(channelName))
                _pendingChannelSubscriptions.Remove(channelName);

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
            }
        }

        /// <summary>
        /// Connect to the Pusher Server.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_connection != null
                && _connection.IsConnected)
            {
                return;
            }

            // Prevent multiple concurrent connections
            await _mutexLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Ensure we only ever attempt to connect once
                if (_connection != null
                    && _connection.IsConnected)
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
        /// Subscribes to the given channel asynchronously, unless the channel already exists, in which case the existing channel will be returned.
        /// </summary>
        /// <param name="channelName">The name of the Channel to subsribe to</param>
        /// <returns>The Channel that is being subscribed to</returns>
        public async Task<Channel> SubscribeAsync(string channelName)
        {
            GuardChannelName(channelName);

            if (AlreadySubscribed(channelName))
            {
                return Channels[channelName];
            }

            _pendingChannelSubscriptions.Add(channelName);

            return await SubscribeToChannel(channelName).ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribes to the given channel asynchronously, unless the channel already exists, in which case the existing channel will be returned.
        /// </summary>
        /// <param name="channelName">The name of the Channel to subsribe to</param>
        /// <typeparam name="MemberT">The type used to deserialize channel member info</typeparam>
        /// <returns>The Channel that is being subscribed to</returns>
        public async Task<GenericPresenceChannel<MemberT>> SubscribePresenceAsync<MemberT>(string channelName)
        {
            GuardChannelName(channelName);

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
                    result = (await SubscribeAsync(channelName, presenceChannel).ConfigureAwait(false)) as GenericPresenceChannel<MemberT>;
                }
            }
            else
            {
                result = new GenericPresenceChannel<MemberT>(channelName, this, Options);
                if (Channels.TryAdd(channelName, result))
                {
                    result = (await SubscribeAsync(channelName, result).ConfigureAwait(false)) as GenericPresenceChannel<MemberT>;
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
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                return channel;
            }

            return null;
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

        private async Task<Channel> SubscribeToChannel(string channelName)
        {
            if (Channels.TryGetValue(channelName, out Channel channel))
            {
                if (channel.IsSubscribed)
                {
                    return channel;
                }
            }

            return await SubscribeAsync(channelName, channel).ConfigureAwait(false);
        }

        private async Task<Channel> SubscribeAsync(string channelName, Channel channel)
        {
            ChannelTypes channelType;
            if (channel != null)
            {
                channelType = channel.ChannelType;
            }
            else
            {
                channelType = Channel.GetChannelType(channelName);
                channel = CreateChannel(channelType, channelName);
            }

            if (State == ConnectionState.Connected)
            {
                if (channelType == ChannelTypes.Presence || channelType == ChannelTypes.Private)
                {
                    var jsonAuth = _options.Authorizer.Authorize(channelName, _connection.SocketId);

                    var template = new { auth = string.Empty, channel_data = string.Empty };
                    dynamic messageObj = JsonConvert.DeserializeAnonymousType(jsonAuth, template);
                    string message = JsonConvert.SerializeObject(new
                    {
                        @event = Constants.CHANNEL_SUBSCRIBE,
                        data = new
                        {
                            channel = channelName,
                            messageObj.auth,
                            messageObj.channel_data
                        }
                    });

                    await _connection.SendAsync(message).ConfigureAwait(false);
                }
                else
                {
                    // No need for auth details. Just send subscribe event
                    string message = JsonConvert.SerializeObject(new
                    {
                        @event = Constants.CHANNEL_SUBSCRIBE,
                        data = new { channel = channelName }
                    });
                    await _connection.SendAsync(message).ConfigureAwait(false);
                }
            }

            return channel;
        }

        private Channel CreateChannel(ChannelTypes type, string channelName)
        {
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

            if (Channels.TryAdd(channelName, result))
            {
                return result;
            }

            return Channels[channelName];
        }

        private void AuthEndpointCheck()
        {
            if (_options.Authorizer == null)
            {
                var pusherException = new PusherException("You must set a ChannelAuthorizer property to use private or presence channels", ErrorCodes.ChannelAuthorizerNotSet);
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

        async Task ITriggerChannels.Unsubscribe(string channelName)
        {
            if (_connection.IsConnected)
            {
                if (Channels.ContainsKey(channelName))
                {
                    await _connection.SendAsync(JsonConvert.SerializeObject(new
                    {
                        @event = Constants.CHANNEL_UNSUBSCRIBE,
                        data = new { channel = channelName }
                    })).ConfigureAwait(false);
                }
            }
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

        private bool AlreadySubscribed(string channelName)
        {
            return _pendingChannelSubscriptions.Contains(channelName) || (Channels.ContainsKey(channelName) && Channels[channelName].IsSubscribed);
        }

        private void MarkChannelsAsUnsubscribed()
        {
            foreach (var channel in Channels)
            {
                channel.Value.Unsubscribe();
            }
        }

        private void SubscribeExistingChannels()
        {
            foreach (var channel in Channels)
            {
                _ = SubscribeToChannel(channel.Key).Result;
            }
        }

        private static void GuardChannelName(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }
        }
    }
}
