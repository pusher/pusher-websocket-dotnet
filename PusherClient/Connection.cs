using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace PusherClient
{
    internal class Connection : IConnection
    {
        private WebSocket _websocket;

        private readonly string _url;
        private readonly IPusher _pusher;

        private int _backOffMillis;

        private static readonly int MAX_BACKOFF_MILLIS = 10000;
        private static readonly int BACK_OFF_MILLIS_INCREMENT = 1000;

        private SemaphoreSlim _connectionSemaphore;
        private Exception _currentError;

        public string SocketId { get; private set; }

        public ConnectionState State { get; private set; } = ConnectionState.Uninitialized;

        public bool IsConnected => State == ConnectionState.Connected;

        public Connection(IPusher pusher, string url)
        {
            _pusher = pusher;
            _url = url;
        }

        public async Task ConnectAsync()
        {
            _connectionSemaphore = new SemaphoreSlim(0, 1);
            try
            {
                _currentError = null;
                if (_pusher.IsTracingEnabled)
                {
                    Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Connecting to: {_url}");
                }

                ChangeState(ConnectionState.Connecting);

                _websocket = new WebSocket(_url)
                {
                    EnableAutoSendPing = true,
                    AutoSendPingInterval = 1
                };

                await Task.Run(() =>
                {
                    _websocket.Error += WebsocketConnectionError;
                    _websocket.MessageReceived += WebsocketMessageReceived;
                    _websocket.Open();
                }).ConfigureAwait(false);

                await _connectionSemaphore.WaitAsync().ConfigureAwait(false);
                if (_currentError != null)
                {
                    throw _currentError;
                }
            }
            finally
            {
                if (_websocket != null)
                {
                    _websocket.Error -= WebsocketConnectionError;
                }

                _connectionSemaphore.Dispose();
                _connectionSemaphore = null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_websocket != null && State != ConnectionState.Disconnected)
            {
                if (_pusher.IsTracingEnabled)
                {
                    Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Disconnecting from: {_url}");
                }

                ChangeState(ConnectionState.Disconnecting);

                await Task.Run(() =>
                {
                    _websocket.Close();
                }).ConfigureAwait(false);

                DisposeWebsocket();
            }
        }

        public async Task<bool> SendAsync(string message)
        {
            if (IsConnected)
            {
                if (_pusher.IsTracingEnabled)
                {
                    Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Sending:{Environment.NewLine}{message}");
                }

                await Task.Run(() => _websocket.Send(message));
                return true;
            }

            if (_pusher.IsTracingEnabled)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"No active connection, did not send:{Environment.NewLine}{message}");
            }

            return false;
        }

        private void DisposeWebsocket()
        {
            _currentError = null;
            _websocket.Closed -= WebsocketAutoReconnect;
            _websocket.Dispose();
            _websocket = null;
            ChangeState(ConnectionState.Disconnected);
        }

        private void WebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (_pusher.IsTracingEnabled)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Websocket message received:{Environment.NewLine}{e.Message}");
            }

            // DeserializeAnonymousType will throw and error when an error comes back from pusher
            // It stems from the fact that the data object is a string normally except when an error is sent back
            // then it's an object.

            // bad:  "{\"event\":\"pusher:error\",\"data\":{\"code\":4201,\"message\":\"Pong reply not received\"}}"
            // good: "{\"event\":\"pusher:error\",\"data\":\"{\\\"code\\\":4201,\\\"message\\\":\\\"Pong reply not received\\\"}\"}";

            var jObject = JObject.Parse(e.Message);

            if (jObject["data"] != null && jObject["data"].Type != JTokenType.String)
                jObject["data"] = jObject["data"].ToString(Formatting.None);

            var jsonMessage = jObject.ToString(Formatting.None);
            var template = new { @event = string.Empty, data = string.Empty, channel = string.Empty };

            var message = JsonConvert.DeserializeAnonymousType(jsonMessage, template);

            var eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);

            if (jObject["data"] != null)
                eventData["data"] = jObject["data"].ToString(); // undo any kind of deserialisation of the data property

            var receivedEvent = new PusherEvent(eventData, jsonMessage);

            _pusher.EmitPusherEvent(message.@event, receivedEvent);

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

                    case Constants.CHANNEL_SUBSCRIPTION_SUCCEEDED:
                        _pusher.SubscriptionSuceeded(message.channel, message.data);
                        break;

                    case Constants.CHANNEL_SUBSCRIPTION_ERROR:
                        RaiseError(new PusherException($"Subscription error received on channel: {message.channel}", ErrorCodes.SubscriptionError));
                        break;

                    case Constants.CHANNEL_MEMBER_ADDED:
                        _pusher.AddMember(message.channel, message.data);
                        break;

                    case Constants.CHANNEL_MEMBER_REMOVED:
                        _pusher.RemoveMember(message.channel, message.data);
                        break;
                }
            }
            else // Assume channel event
            {
                _pusher.EmitChannelEvent(message.channel, message.@event, receivedEvent);
            }
        }

        private void WebsocketConnectionError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (_pusher.IsTracingEnabled)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Error, 0, $"Error when connecting:{Environment.NewLine}{e.Exception}");
            }

            _currentError = e.Exception;
            _connectionSemaphore?.Release();
        }

        private void ParseConnectionEstablished(string data)
        {
            var template = new { socket_id = string.Empty };
            var message = JsonConvert.DeserializeAnonymousType(data, template);
            SocketId = message.socket_id;

            ChangeState(ConnectionState.Connected);
            _connectionSemaphore?.Release();
            _websocket.Closed += WebsocketAutoReconnect;
            _backOffMillis = 0;
        }

        private void WebsocketAutoReconnect(object sender, EventArgs e)
        {
            DisposeWebsocket();
            _websocket = new WebSocket(_url)
            {
                EnableAutoSendPing = true,
                AutoSendPingInterval = 1
            };

            Task.Run(() =>
            {
                try
                {
                    _backOffMillis = Math.Min(MAX_BACKOFF_MILLIS, _backOffMillis + BACK_OFF_MILLIS_INCREMENT);
                    if (_pusher.IsTracingEnabled)
                    {
                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Waiting " + _backOffMillis.ToString() + "ms before attempting to reconnect");
                    }

                    ChangeState(ConnectionState.WaitingToReconnect);
                    Task.WaitAll(Task.Delay(_backOffMillis));

                    if (_websocket != null && State != ConnectionState.Disconnected)
                    {
                        if (_pusher.IsTracingEnabled)
                        {
                            Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Attempting websocket reconnection");
                        }

                        ChangeState(ConnectionState.Connecting);
                        _websocket.MessageReceived += WebsocketMessageReceived;
                        _websocket.Closed += WebsocketAutoReconnect;
                        _websocket.Open();
                    }
                }
                catch(Exception error)
                {
                    RaiseError(new ReconnectionException(error));
                }
            });
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

            RaiseError(new PusherException(parsed.message, error));
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;
            _pusher.ConnectionStateChanged(state);
        }

        private void RaiseError(PusherException error)
        {
            _currentError = error;
            _pusher.ErrorOccured(error);
            _connectionSemaphore?.Release();
        }
    }
}
