using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

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
        string _applicationKey = null;
        PusherOptions _options = null;

        public string Host = "ws.pusherapp.com";
        private Connection _connection = null;
        private ErrorEventHandler _errorEvent;

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
        public Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();

        #region Properties

        public string SocketID {
            get
            {
                return _connection.SocketID;
            }
        }

        public ConnectionState State
        {
            get
            {
                return _connection.State;
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
            // Check current connection state
            if (_connection != null)
            {
                switch (_connection.State)
                {
                    case ConnectionState.Connected:
                        Trace.TraceEvent(TraceEventType.Warning, 0, "Attempt to connect when connection is already in 'Connected' state. New attempt has been ignored.");
                        break;
                    case ConnectionState.Connecting:
                        Trace.TraceEvent(TraceEventType.Warning, 0, "Attempt to connect when connection is already in 'Connecting' state. New attempt has been ignored.");
                        break;
                    case ConnectionState.Failed:
                        Trace.TraceEvent(TraceEventType.Error, 0, "Cannot attempt re-connection once in 'Failed' state");
                        RaiseError(new PusherException("Cannot attempt re-connection once in 'Failed' state", ErrorCodes.ConnectionFailed));
                        break;
                }
            }

            var scheme = "ws://";

            if (_options.Encrypted)
                scheme = "wss://";

            // TODO: Fallback to secure?

            string url = String.Format("{0}{1}/app/{2}?protocol={3}&client={4}&version={5}", 
                scheme, this.Host, _applicationKey, Settings.Default.ProtocolVersion, Settings.Default.ClientName,
                Settings.Default.VersionNumber);

            _connection = new Connection(this, url);
            _connection.Connected += _connection_Connected;
            _connection.ConnectionStateChanged +=_connection_ConnectionStateChanged;
            if (_errorEvent != null)
            {
                // subscribe to the connection's error handler
                foreach (ErrorEventHandler handler in _errorEvent.GetInvocationList())
                {
                    _connection.Error += handler;
                }
            }
            _connection.Connect();
            
        }

        public void Disconnect()
        {
            _connection.Disconnect();
        }

        public Channel Subscribe(string channelName)
        {
            if (_connection.State != ConnectionState.Connected)
            {
                var pusherException = new PusherException("You must wait for Pusher to connect before you can subscribe to a channel", ErrorCodes.NotConnected);
                RaiseError(pusherException);
                throw pusherException;
            }

            if (Channels.ContainsKey(channelName))
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
            switch (type)
            {
                case ChannelTypes.Public:
                    Channels.Add(channelName, new Channel(channelName, this));
                    break;
                case ChannelTypes.Private:
                    AuthEndpointCheck();
                    Channels.Add(channelName, new PrivateChannel(channelName, this));
                    break;
                case ChannelTypes.Presence:
                    AuthEndpointCheck();
                    Channels.Add(channelName, new PresenceChannel(channelName, this));
                    break;
            }

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

        public void Send(string eventName, object data, string channel = null)
        {
            _connection.Send(JsonConvert.SerializeObject(new { @event = eventName, channel = channel, data = data }));
        }

        #endregion

        #region Internal Methods

        internal void Trigger(string channelName, string eventName, object obj)
        {
            _connection.Send(JsonConvert.SerializeObject(new { @event = eventName, channel = channelName, data = obj }));
        }

        internal void Unsubscribe(string channelName)
        {
            _connection.Send(JsonConvert.SerializeObject(new { @event = Constants.CHANNEL_UNSUBSCRIBE, data = new { channel = channelName } }));
        }

        #endregion

        #region Event Handlers

        private void _connection_ConnectionStateChanged(object sender, ConnectionState state)
        {
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
    }
}