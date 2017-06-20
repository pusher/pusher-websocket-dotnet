using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace PusherClient
{
    public class ConnectionAsync
    {
        private readonly IPusher _pusher;
        private readonly string _url;
        private bool _allowReconnect;
        private WebSocket _websocket;

        private readonly TaskCompletionSource<bool> _connectionTaskComplete = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> _disconnectionTaskComplete = new TaskCompletionSource<bool>();
        private string _socketId;

        internal ConnectionAsync(IPusher pusher, string url)
        {
            _pusher = pusher;
            _url = url;
        }

        public ConnectionState State { get; private set; } = ConnectionState.Uninitialized;

        public Task<bool> Connect()
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Connecting to: {_url}");

            ChangeState(ConnectionState.Initialized);
            _allowReconnect = true;

            _websocket = new WebSocket(_url)
            {
                EnableAutoSendPing = true,
                AutoSendPingInterval = 1
            };

            _websocket.MessageReceived += OnWebsocketOnMessageReceived;
            _websocket.Opened += OnWebsocketOnOpened;
            _websocket.Error += OnWebsocketOnError;
            _websocket.Closed += OnWebsocketOnClosed;

            _websocket.Open();

            return _connectionTaskComplete.Task;
        }

        internal Task<bool> Disconnect()
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Disconnecting from: {_url}");

            ChangeState(ConnectionState.Disconnecting);

            _allowReconnect = false;

            _websocket.Close();

            ChangeState(ConnectionState.Disconnected);

            return _disconnectionTaskComplete.Task;
        }

        private void OnWebsocketOnClosed(object sender, EventArgs args)
        {
            _websocket.MessageReceived -= OnWebsocketOnMessageReceived;
            _websocket.Opened -= OnWebsocketOnOpened;
            _websocket.Error -= OnWebsocketOnError;
            _websocket.Closed -= OnWebsocketOnClosed;

            Pusher.Trace.TraceEvent(TraceEventType .Information, 0, "Websocket closed OK.");
            _disconnectionTaskComplete.SetResult(true);
        }

        private void OnWebsocketOnError(object sender, ErrorEventArgs args)
        {
            if (_connectionTaskComplete != null)
                _connectionTaskComplete.TrySetException(args.Exception);
            else
            {
                // raise an error
            }
        }

        private void OnWebsocketOnOpened(object sender, EventArgs args)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Websocket opened OK.");
        }

        private void OnWebsocketOnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Websocket message received: " + args.Message);

            Debug.WriteLine(args.Message);

            // DeserializeAnonymousType will throw and error when an error comes back from pusher
            // It stems from the fact that the data object is a string normally except when an error is sent back
            // then it's an object.

            // bad:  "{\"event\":\"pusher:error\",\"data\":{\"code\":4201,\"message\":\"Pong reply not received\"}}"
            // good: "{\"event\":\"pusher:error\",\"data\":\"{\\\"code\\\":4201,\\\"message\\\":\\\"Pong reply not received\\\"}\"}";

            var jObject = JObject.Parse(args.Message);

            if (jObject["data"] != null && jObject["data"].Type != JTokenType.String)
                jObject["data"] = jObject["data"].ToString(Formatting.None);

            var jsonMessage = jObject.ToString(Formatting.None);
            var template = new { @event = string.Empty, data = string.Empty, channel = string.Empty };

            var message = JsonConvert.DeserializeAnonymousType(jsonMessage, template);

            _pusher.EmitPusherEvent(message.@event, message.data);

            if (message.@event.StartsWith(Constants.PUSHER_MESSAGE_PREFIX))
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
                }
            }
            else // Assume channel event
            {
                _pusher.EmitChannelEvent(message.channel, message.@event, message.data);
            }
        }

        private void ParseError(string data)
        {
            var template = new { message = string.Empty, code = (int?)null };
            var parsed = JsonConvert.DeserializeAnonymousType(data, template);

            ErrorCodes error = ErrorCodes.Unkown;

            if (parsed.code != null && Enum.IsDefined(typeof(ErrorCodes), parsed.code))
            {
                error = (ErrorCodes)parsed.code;
            }

            //RaiseError(new PusherException(parsed.message, error));
            _connectionTaskComplete.TrySetResult(false);
        }

        private void ParseConnectionEstablished(string data)
        {
            var template = new { socket_id = string.Empty };
            var message = JsonConvert.DeserializeAnonymousType(data, template);
            _socketId = message.socket_id;

            _connectionTaskComplete.TrySetResult(true);

            ChangeState(ConnectionState.Connected);
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;
            _pusher.ConnectionStateChanged(state);
        }
    }
}
