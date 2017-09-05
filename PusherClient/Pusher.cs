using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Diagnostics;

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
    // TODO: NUGET Package
    // TODO: Ping & pong, are these handled by the Webscoket library out of the box?
    // TODO: Add assembly info file?
    // TODO: Implement connection fallback strategy

    // A delegate type for hooking up change notifications.
    public delegate void ErrorEventHandler(object sender, PusherException error);
    public delegate void ConnectedEventHandler(object sender);
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionState state);

    public class Pusher : EventEmitter
    {
        // create single TraceSource instance to be used for logging
        public static TraceSource Trace = new TraceSource("Pusher");

        const int PROTOCOL_NUMBER = 5;
        private readonly string _applicationKey = null;
        private readonly PusherOptions _options = null;

        private Connection _connection = null;
        private ErrorEventHandler _errorEvent;

        private readonly object _lockingObject = new object();

        public event ErrorEventHandler Error
        {
            add
            {
                _errorEvent += value;
                if (_connection != null)
                {
                    _connection.Error += value;
                }
            }
            remove
            {
                _errorEvent -= value;
                if (_connection != null)
                {
                    _connection.Error -= value;
                }
            }
        }

        public event ConnectedEventHandler Connected;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();

        #region Properties

        public string SocketID {
            get
            {
                return (_connection != null? _connection.SocketID: null);
            }
        }

        public ConnectionState State
        {
            get
            {
                return (_connection != null? _connection.State: ConnectionState.Disconnected);
            }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="Pusher" /> class.
        /// </summary>
        /// <param name="applicationKey">The application key.</param>
        /// <param name="options">The options.</param>
        public Pusher(string applicationKey, PusherOptions options = null)
        {
            _applicationKey = applicationKey;

            if (options == null)
                _options = new PusherOptions() { Encrypted = false };
            else
                _options = options;
        }

        #region Public Methods

        public void Connect()
        {
            // Prevent multiple concurrent connections
            lock(_lockingObject)
            {
                // Ensure we only ever attempt to connect once
                if (_connection != null)
                {
                  Trace.TraceEvent(TraceEventType.Warning, 0, "Attempt to connect when another connection has already started. New attempt has been ignored.");
                  return;
                }

                var scheme = "ws://";

                if (_options.Encrypted)
                    scheme = "wss://";

                // TODO: Fallback to secure?

                string url = String.Format("{0}{1}/app/{2}?protocol={3}&client={4}&version={5}",
                    scheme, _options.Host, _applicationKey, Settings.Default.ProtocolVersion, Settings.Default.ClientName,
                    Settings.Default.VersionNumber);

                _connection = new Connection(this, url);
                RegisterEventsOnConnection();
                _connection.Connect();
            }
        }

        private void RegisterEventsOnConnection()
        {
            _connection.Connected += _connection_Connected;
            _connection.ConnectionStateChanged += _connection_ConnectionStateChanged;
            if (_errorEvent != null)
            {
                // subscribe to the connection's error handler
                foreach (ErrorEventHandler handler in _errorEvent.GetInvocationList())
                {
                    _connection.Error += handler;
                }
            }
        }

        public void Disconnect()
        {
            UnregisterEventsOnDisconnection();
            MarkChannelsAsUnsubscribed();
            _connection.Disconnect();
            _connection = null;
        }

        private void UnregisterEventsOnDisconnection()
        {
            if (_connection != null)
            {
                _connection.Connected -= _connection_Connected;
                _connection.ConnectionStateChanged -= _connection_ConnectionStateChanged;

                if (_errorEvent != null)
                {
                    // unsubscribe to the connection's error handler
                    foreach (ErrorEventHandler handler in _errorEvent.GetInvocationList())
                    {
                        _connection.Error -= handler;
                    }
                }
            }
        }

        public Channel Subscribe(string channelName)
        {
            if (AlreadySubscribed(channelName))
            {
                Trace.TraceEvent(TraceEventType.Warning, 0, "Channel '" + channelName + "' is already subscribed to. Subscription event has been ignored.");
                return Channels[channelName];
            }

            // If private or presence channel, check that auth endpoint has been set
            var chanType = ChannelTypes.Public;

            if (channelName.ToLower().StartsWith("private-"))
                chanType = ChannelTypes.Private;
            else if (channelName.ToLower().StartsWith("presence-"))
                chanType = ChannelTypes.Presence;

            return SubscribeToChannel(chanType, channelName);
        }

        private Channel SubscribeToChannel(ChannelTypes type, string channelName)
        {
            if (!Channels.ContainsKey(channelName))
                CreateChannel(type, channelName);

            if (State == ConnectionState.Connected)
            {
                if (type == ChannelTypes.Presence || type == ChannelTypes.Private)
                {
                    string jsonAuth = _options.Authorizer.Authorize(channelName, _connection.SocketID);

                    var template = new { auth = String.Empty, channel_data = String.Empty };
                    var message = JsonConvert.DeserializeAnonymousType(jsonAuth, template);

                    _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_SUBSCRIBE, data = new { channel = channelName, auth = message.auth, channel_data = message.channel_data } }));
                }
                else
                {
                    // No need for auth details. Just send subscribe event
                    _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_SUBSCRIBE, data = new { channel = channelName } }));
                }
            }

            return Channels[channelName];
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
                    Channels[channelName] = new PresenceChannel(channelName, this);
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

        #endregion

        #region Internal Methods

        internal void Trigger(string channelName, string eventName, object obj)
        {
            _connection.Send(JsonConvert.SerializeObject(new { @event = eventName, channel = channelName, data = obj }));
        }

        internal void Unsubscribe(string channelName)
        {
            if (_connection.State == ConnectionState.Connected)
              _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_UNSUBSCRIBE, data = new { channel = channelName } }));
        }

        #endregion

        #region Event Handlers

        private void _connection_ConnectionStateChanged(object sender, ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Disconnected:
                    MarkChannelsAsUnsubscribed();
                    break;
                case ConnectionState.Connected:
                    SubscribeExistingChannels();
                    break;
            }

            if (ConnectionStateChanged != null)
                ConnectionStateChanged(sender, state);
        }

        void _connection_Connected(object sender)
        {
            if (this.Connected != null)
                this.Connected(sender);
        }

        private void RaiseError(PusherException error)
        {
            var handler = _errorEvent;
            if (handler != null) handler(this, error);
        }

        #endregion

        private bool AlreadySubscribed(string channelName)
        {
            return Channels.ContainsKey(channelName) && Channels[channelName].IsSubscribed;
        }

        internal void MarkChannelsAsUnsubscribed()
        {
            foreach (var channel in Channels)
            {
                channel.Value.Unsubscribe();
            }

        }

        internal void SubscribeExistingChannels()
        {
            foreach (var channel in Channels)
            {
                Subscribe(channel.Key);
            }
        }

    }
}
