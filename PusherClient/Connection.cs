using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace PusherClient
{
    internal class Connection
    {
        private WebSocket _websocket = null;
        private string _socketId = null;
        private string _url = null;
        private Pusher _pusher = null;
        private ConnectionState _state = ConnectionState.Initialized;
        private bool _allowReconnect = true;

        public event ErrorEventHandler Error;
        public event ConnectedEventHandler Connected;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;

        #region Properties

        internal string SocketID
        {
            get
            {
                return _socketId;
            }
        }
        internal ConnectionState State
        {
            get
            {
                return _state;
            }
        }

        #endregion

        public Connection(Pusher pusher, string url)
        {
            this._url = url;
            this._pusher = pusher;
        }

        #region Internal Methods

        internal void Connect()
        {
            // TODO: Handle and test disconnection / errors etc
            // TODO: Add 'connecting_in' event

            ChangeState(ConnectionState.Connecting);
            _allowReconnect = true;

            _websocket = new WebSocket(_url);
            _websocket.EnableAutoSendPing = true;
            _websocket.AutoSendPingInterval = 1;
            _websocket.Opened += websocket_Opened;
            _websocket.Error += websocket_Error;
            _websocket.Closed += websocket_Closed;
            _websocket.MessageReceived += websocket_MessageReceived;
            _websocket.Open();
        }

        internal void Disconnect()
        {
            _allowReconnect = false;
            _websocket.Close();
            ChangeState(ConnectionState.Disconnected);
        }

        internal void Send(string message)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Sending: " + message);
            Debug.WriteLine("Sending: " + message);
            _websocket.Send(message);
        }

        #endregion

        #region Private Methods

        private void ChangeState(ConnectionState state)
        {
            this._state = state;

            if (ConnectionStateChanged != null)
                ConnectionStateChanged(this, this._state);
        }

        private void RaiseError(PusherException error)
        {
            // if a handler is registerd, use it, otherwise throw
            var handler = Error;
            if (handler != null) handler(this, error);
            else throw error;
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Websocket message received: " + e.Message);

            Debug.WriteLine(e.Message);

            // DeserializeAnonymousType will throw and error when an error comes back from pusher
            // It stems from the fact that the data object is a string normally except when an error is sent back
            // then it's an object.

            // bad:  "{\"event\":\"pusher:error\",\"data\":{\"code\":4201,\"message\":\"Pong reply not received\"}}"
            // good: "{\"event\":\"pusher:error\",\"data\":\"{\\\"code\\\":4201,\\\"message\\\":\\\"Pong reply not received\\\"}\"}";

            var jObject = JObject.Parse(e.Message);

            if (jObject["data"] != null && jObject["data"].Type != JTokenType.String)
                jObject["data"] = jObject["data"].ToString(Formatting.None);

            string jsonMessage = jObject.ToString(Formatting.None);
            var template = new { @event = String.Empty, data = String.Empty, channel = String.Empty };

            //var message = JsonConvert.DeserializeAnonymousType(e.Message, template);
            var message = JsonConvert.DeserializeAnonymousType(jsonMessage, template);

            _pusher.EmitEvent(message.@event, message.data);

            if (message.@event.StartsWith("pusher"))
            {
                // Assume Pusher event
                switch (message.@event)
                {
                    case Constants.ERROR:
                        ParseError(message.data);
                        break;

                    case Constants.CONNECTION_ESTABLISHED:
                        ParseConnectionEstablished(message.data);
                        break;

                    case Constants.CHANNEL_SUBSCRIPTION_SUCCEEDED:

                        if (_pusher.Channels.ContainsKey(message.channel))
                        {
                            var channel = _pusher.Channels[message.channel];
                            channel.SubscriptionSucceeded(message.data);
                        }

                        break;

                    case Constants.CHANNEL_SUBSCRIPTION_ERROR:

                        RaiseError(new PusherException("Error received on channel subscriptions: " + e.Message, ErrorCodes.SubscriptionError));
                        break;

                    case Constants.CHANNEL_MEMBER_ADDED:

                        // Assume channel event
                        if (_pusher.Channels.ContainsKey(message.channel))
                        {
                            var channel = _pusher.Channels[message.channel];

                            if (channel is PresenceChannel)
                            {
                                ((PresenceChannel)channel).AddMember(message.data);
                                break;
                            }
                        }

                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
                        break;

                    case Constants.CHANNEL_MEMBER_REMOVED:

                        // Assume channel event
                        if (_pusher.Channels.ContainsKey(message.channel))
                        {
                            var channel = _pusher.Channels[message.channel];

                            if (channel is PresenceChannel)
                            {
                                ((PresenceChannel)channel).RemoveMember(message.data);
                                break;
                            }
                        }

                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
                        break;

                }
            }
            else
            {
                // Assume channel event
                if (_pusher.Channels.ContainsKey(message.channel))
                    _pusher.Channels[message.channel].EmitEvent(message.@event, message.data);
            }

            
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Websocket opened OK.");
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Websocket connection has been closed");

            ChangeState(ConnectionState.Disconnected);

            if(_allowReconnect)
                Connect();
        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Error, 0, "Error: " + e.Exception);

            // TODO: What happens here? Do I need to re-connect, or do I just log the issue?
        }

        private void ParseConnectionEstablished(string data)
        {
            var template = new { socket_id = String.Empty };
            var message = JsonConvert.DeserializeAnonymousType(data, template);
            _socketId = message.socket_id;

            ChangeState(ConnectionState.Connected);

            if (Connected != null)
                Connected(this);
        }

        private void ParseError(string data)
        {
            var template = new { message = String.Empty, code = 0 };
            var parsed = JsonConvert.DeserializeAnonymousType(data, template);

            ErrorCodes error = ErrorCodes.Unkown;

            if (Enum.IsDefined(typeof(ErrorCodes), parsed.code))
                error = (ErrorCodes)parsed.code;

            RaiseError(new PusherException(parsed.message, error));
        }

        #endregion
    }
}
