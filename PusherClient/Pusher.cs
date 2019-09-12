using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Fire when an error occurs
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// The TraceSource instance to be used for logging
        /// </summary>
        public static TraceSource Trace = new TraceSource(nameof(Pusher));

        private readonly string _applicationKey;
        private readonly PusherOptions _options;
        private readonly List<string> _pendingChannelSubscriptions = new List<string>();

        /// <summary>
        /// Tracks the member info types used to create each non-dynamic presence channel
        /// </summary>
        private readonly ConcurrentDictionary<string, Tuple<Type, Func<Channel>>> _presenceChannelFactories 
            = new ConcurrentDictionary<string, Tuple<Type, Func<Channel>>>();

        private Connection _connection;

        /// <summary>
        /// Gets the Socket ID
        /// </summary>
        public string SocketID => _connection?.SocketId;

        /// <summary>
        /// Gets the current connection state
        /// </summary>
        public ConnectionState State => _connection?.State ?? ConnectionState.NotConnected;

        /// <summary>
        /// Gets the channels in use by the Client
        /// </summary>
        public ConcurrentDictionary<string, Channel> Channels { get; private set; } = new ConcurrentDictionary<string, Channel>();

        /// <summary>
        /// Gets the Options in use by the Client
        /// </summary>
        internal PusherOptions Options => _options;

        SemaphoreSlim _mutexLock = new SemaphoreSlim(1);

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

            _options = options ?? new PusherOptions { Encrypted = false };
        }

        void IPusher.ConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                SubscribeExistingChannels();

                Connected?.Invoke(this);
            }
            else if (state == ConnectionState.Disconnected)
            {
                MarkChannelsAsUnsubscribed();

                Disconnected?.Invoke(this);

                ConnectionStateChanged?.Invoke(this, state);
            }
            else
            {
                ConnectionStateChanged?.Invoke(this, state);
            }
        }

        void IPusher.ErrorOccured(PusherException pusherException)
        {
            RaiseError(pusherException);
        }

        void IPusher.EmitPusherEvent(string eventName, string data)
        {
            EmitEvent(eventName, data);
        }

        void IPusher.EmitChannelEvent(string channelName, string eventName, string data)
        {
            if (Channels.ContainsKey(channelName))
            {
                Channels[channelName].EmitEvent(eventName, data);
            }
        }

        void IPusher.AddMember(string channelName, string member)
        {
            if (Channels.Keys.Contains(channelName) && Channels[channelName] is PresenceChannel)
            {
                ((PresenceChannel)Channels[channelName]).AddMember(member);
            }
        }

        void IPusher.RemoveMember(string channelName, string member)
        {
            if (Channels.Keys.Contains(channelName) && Channels[channelName] is PresenceChannel)
            {
                ((PresenceChannel)Channels[channelName]).RemoveMember(member);
            }
        }

        void IPusher.SubscriptionSuceeded(string channelName, string data)
        {
            if (_pendingChannelSubscriptions.Contains(channelName))
                _pendingChannelSubscriptions.Remove(channelName);

            if (Channels.Keys.Contains(channelName))
            {
                Channels[channelName].SubscriptionSucceeded(data);
            }
        }

        /// <summary>
        /// Start the connection to the Pusher Server asynchronously.  When completed, the <see cref="Connected"/> event will fire.
        /// </summary>
        public async Task<ConnectionState> ConnectAsync()
        {
            if (_connection != null)
            {
                //Trace.TraceEvent(TraceEventType.Warning, 0, ErrorConstants.ConnectionAlreadyConnected);
                return ConnectionState.AlreadyConnected;
            }

            // Prevent multiple concurrent connections
            var connectionResult = ConnectionState.Connecting;

            await _mutexLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Ensure we only ever attempt to connect once
                if (_connection != null)
                {
                    //Trace.TraceEvent(TraceEventType.Warning, 0, ErrorConstants.ConnectionAlreadyConnected);
                    return ConnectionState.AlreadyConnected;
                }

                // TODO: Fallback to secure?

                var url = ConstructUrl();

                _connection = new Connection(this, url);
                connectionResult = await _connection.Connect();
            }
            finally
            {
                _mutexLock.Release();
            }

            return connectionResult;
        }

        private string ConstructUrl()
        {
            var scheme = _options.Encrypted ? Constants.SECURE_SCHEMA : Constants.INSECURE_SCHEMA;

            return $"{scheme}{_options.Host}/app/{_applicationKey}?protocol=5&client=pusher-dotnet-client&version=0.0.1";
        }

        /// <summary>
        /// Start the disconnection from the Pusher Server asynchronously.  When completed, the <see cref="Disconnected"/> event will fire.
        /// </summary>
        public async Task<ConnectionState> DisconnectAsync()
        {
            ConnectionState connectionResult = ConnectionState.Disconnecting;

            if (_connection != null)
            {
                MarkChannelsAsUnsubscribed();
                connectionResult = await _connection.Disconnect();
            }
            else
            {
                connectionResult = ConnectionState.Disconnected;
            }

            return connectionResult;
        }

        /// <summary>
        /// Subscribes to the given channel asynchronously, unless the channel already exists, in which case the existing channel will be returned.
        /// </summary>
        /// <param name="channelName">The name of the Channel to subsribe to</param>
        /// <returns>The Channel that is being subscribed to</returns>
        public async Task<Channel> SubscribeAsync(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException("The channel name cannot be null or whitespace", nameof(channelName));
            }

            if (AlreadySubscribed(channelName))
            {
                //Trace.TraceEvent(TraceEventType.Warning, 0, "Channel '" + channelName + "' is already subscribed to. Subscription event has been ignored.");
                return Channels[channelName];
            }

            _pendingChannelSubscriptions.Add(channelName);

            return await SubscribeToChannel(channelName);
        }

        /// <summary>
        /// Subscribes to the given channel asynchronously, unless the channel already exists, in which case the existing channel will be returned.
        /// </summary>
        /// <param name="channelName">The name of the Channel to subsribe to</param>
        /// <typeparam name="MemberT">The type used to deserialize channel member info</typeparam>
        /// <returns>The Channel that is being subscribed to</returns>
        public async Task<GenericPresenceChannel<MemberT>> SubscribePresenceAsync<MemberT>(string channelName)
        {
            var channelType = GetChannelType(channelName);
            if (channelType != ChannelTypes.Presence)
                throw new ArgumentException("The channel name must be refer to a presence channel", nameof(channelName));

            // We need to keep track of the type we want the channel to be, in case it gets created or re-created later.
            _presenceChannelFactories.AddOrUpdate(channelName, 
                (_) => Tuple.Create<Type, Func<Channel>>(typeof(MemberT), 
                    () => new GenericPresenceChannel<MemberT>(channelName, this)), 
                (_, existing) =>
                {
                    if (existing.Item1 != typeof(MemberT))
                        throw new InvalidOperationException($"Cannot change channel member type; was previously defined as {existing.Item1.Name}");
                    return existing;
                });

            var channel = await SubscribeAsync(channelName);

            var result = channel as GenericPresenceChannel<MemberT>;
            if (result == null)
            {
                if (channel is PresenceChannel)
                    throw new InvalidOperationException("This presence channel has already been created without specifying the member info type");
                else
                    throw new InvalidOperationException($"The presence channel found is an unexpected type: {channel.GetType().Name}");
            }

            return result;
        }

        private async Task<Channel> SubscribeToChannel(string channelName)
        {
            var channelType = GetChannelType(channelName);

            if (!Channels.ContainsKey(channelName))
                CreateChannel(channelType, channelName);

            if (State == ConnectionState.Connected)
            {
                if (channelType == ChannelTypes.Presence || channelType == ChannelTypes.Private)
                {
                    var jsonAuth = _options.Authorizer.Authorize(channelName, _connection.SocketId);

                    var template = new { auth = string.Empty, channel_data = string.Empty };
                    var message = JsonConvert.DeserializeAnonymousType(jsonAuth, template);

                    await _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_SUBSCRIBE, data = new { channel = channelName, auth = message.auth, channel_data = message.channel_data } }));
                }
                else
                {
                    // No need for auth details. Just send subscribe event
                    await _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_SUBSCRIBE, data = new { channel = channelName } }));
                }
            }

            return Channels[channelName];
        }

        private static ChannelTypes GetChannelType(string channelName)
        {
            // If private or presence channel, check that auth endpoint has been set
            var channelType = ChannelTypes.Public;

            if (channelName.ToLowerInvariant().StartsWith(Constants.PRIVATE_CHANNEL))
            {
                channelType = ChannelTypes.Private;
            }
            else if (channelName.ToLowerInvariant().StartsWith(Constants.PRESENCE_CHANNEL))
            {
                channelType = ChannelTypes.Presence;
            }
            return channelType;
        }

        private void CreateChannel(ChannelTypes type, string channelName)
        {
            switch (type)
            {
                case ChannelTypes.Public:
                    Channels[channelName] = new Channel(channelName, this);
                    break;
                case ChannelTypes.Private:
                    AuthEndpointCheck();
                    Channels[channelName] = new PrivateChannel(channelName, this);
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck();

                    Channel channel;
                    if (_presenceChannelFactories.TryGetValue(channelName, out var factory))
                        channel = factory.Item2();
                    else
                        channel = new PresenceChannel(channelName, this);

                    Channels[channelName] = channel;
                    break;
            }
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
            await _connection.Send(JsonConvert.SerializeObject(new { @event = eventName, channel = channelName, data = obj }));
        }

        async Task ITriggerChannels.Unsubscribe(string channelName)
        {
            if (_connection.IsConnected)
            {
                await _connection.Send(JsonConvert.SerializeObject(new
                {
                    @event = Constants.CHANNEL_UNSUBSCRIBE,
                    data = new {channel = channelName}
                }));
            }
        }

        private void RaiseError(PusherException error)
        {
            var handler = Error;

            if (handler != null)
                handler(this, error);
            //else
            //    Pusher.Trace.TraceEvent(TraceEventType.Error, 0, error.ToString());
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
                var result = SubscribeToChannel(channel.Key).Result;
            }
        }
    }
}