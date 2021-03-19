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

        private IList<string> EmitterKeys { get; } = new List<string>
        {
            { nameof(PusherEventEmitter) },
            { nameof(TextEventEmitter) },
            { nameof(DynamicEventEmitter) },
        };

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
            if (_websocket != null)
            {
                if (State != ConnectionState.Disconnected)
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
        }

        public async Task<bool> SendAsync(string message)
        {
            if (IsConnected)
            {
                if (_pusher.IsTracingEnabled)
                {
                    Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Sending:{Environment.NewLine}{message}");
                }

                await Task.Run(() => _websocket.Send(message)).ConfigureAwait(false);
                return true;
            }

            if (_pusher.IsTracingEnabled)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"No active connection, did not send:{Environment.NewLine}{message}");
            }

            return false;
        }

        private static Dictionary<string, object> GetEventPropertiesFromMessage(string messageJson)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            JObject jObject = JObject.Parse(messageJson);
            foreach (JToken child in jObject.Children())
            {
                var item = jObject[child.Path];
                if ("data".Equals(child.Path))
                {
                    if (item.Type != JTokenType.String)
                    {
                        properties[child.Path] = item.ToString(Formatting.None);
                    }
                    else
                    {
                        properties[child.Path] = item.Value<string>();
                    }
                }
                else if (item.Type == JTokenType.String)
                {
                    properties[child.Path] = item.Value<string>();
                }
            }

            return properties;
        }

        private static void EmitEvent(string eventName, IEventBinder binder, string jsonMessage, Dictionary<string, object> message)
        {
            if (binder.HasListeners)
            {
                if (binder is PusherEventEmitter pusherEventEmitter)
                {
                    PusherEvent pusherEvent = new PusherEvent(message, jsonMessage);
                    if (pusherEvent != null)
                    {
                        pusherEventEmitter.EmitEvent(eventName, pusherEvent);
                    }
                }
                else if (binder is TextEventEmitter textEventEmitter)
                {
                    string textEvent = textEventEmitter.ParseJson(jsonMessage);
                    if (textEvent != null)
                    {
                        textEventEmitter.EmitEvent(eventName, textEvent);
                    }
                }
                else if (binder is DynamicEventEmitter dynamicEventEmitter)
                {
                    dynamic dynamicEvent = dynamicEventEmitter.ParseJson(jsonMessage);
                    if (dynamicEvent != null)
                    {
                        dynamicEventEmitter.EmitEvent(eventName, dynamicEvent);
                    }
                }
            }
        }

        private void DisposeWebsocket()
        {
            _currentError = null;
            _websocket.Closed -= WebsocketAutoReconnect;
            _websocket.Error -= WebsocketError;
            _websocket.Dispose();
            _websocket = null;
            ChangeState(ConnectionState.Disconnected);
        }

        private void ProcessChannelEvent(string eventName, string jsonMessage, string channelName, Dictionary<string, object> message)
        {
            foreach (string key in EmitterKeys)
            {
                IEventBinder binder = _pusher.GetChannelEventBinder(key, channelName);
                EmitEvent(eventName, binder, jsonMessage, message);
            }
        }

        private void ProcessEvent(string eventName, string jsonMessage, Dictionary<string, object> message)
        {
            foreach (string key in EmitterKeys)
            {
                IEventBinder binder = _pusher.GetEventBinder(key);
                EmitEvent(eventName, binder, jsonMessage, message);
            }
        }

        private void ProcessPusherEvent(string eventName, string messageData)
        {
            switch (eventName)
            {
                case Constants.CONNECTION_ESTABLISHED:
                    ParseConnectionEstablished(messageData);
                    break;
            }
        }

        private bool ProcessPusherChannelEvent(string eventName, string channelName, string messageData)
        {
            bool processed = true;
            switch (eventName)
            {
                case Constants.CHANNEL_SUBSCRIPTION_SUCCEEDED:
                    _pusher.SubscriptionSuceeded(channelName, messageData);
                    break;

                case Constants.CHANNEL_SUBSCRIPTION_ERROR:
                    _pusher.SubscriptionFailed(channelName, messageData);
                    break;

                case Constants.CHANNEL_MEMBER_ADDED:
                    _pusher.AddMember(channelName, messageData);
                    break;

                case Constants.CHANNEL_MEMBER_REMOVED:
                    _pusher.RemoveMember(channelName, messageData);
                    break;

                default:
                    processed = false;
                    break;
            }

            return processed;
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

            JObject jObject = JObject.Parse(e.Message);
            string rawJson = jObject.ToString(Formatting.None);
            Dictionary<string, object> message = GetEventPropertiesFromMessage(rawJson);
            if (message.ContainsKey("event") && jObject["data"] != null)
            {
                string eventName = (string)message["event"];
                string messageData = string.Empty;
                if (message.ContainsKey("data"))
                {
                    messageData = (string)message["data"];
                }

                if (eventName == Constants.ERROR)
                {
                    ParseError(jObject["data"]);
                }
                else
                {
                    if (message.ContainsKey("channel"))
                    {
                        string channelName = (string)message["channel"];
                        if (!ProcessPusherChannelEvent(eventName, channelName, messageData))
                        {
                            ProcessChannelEvent(eventName, rawJson, channelName, message);
                            ProcessEvent(eventName, rawJson, message);
                        }
                    }
                    else
                    {
                        ProcessPusherEvent(eventName, messageData);
                    }
                }
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

        private void WebsocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (_pusher.IsTracingEnabled)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Error, 0, $"Websocket error:{Environment.NewLine}{e.Exception}");
            }

            RaiseError(new WebsocketException(State, e.Exception));
        }

        private void ParseConnectionEstablished(string messageData)
        {
            JToken jToken = JToken.Parse(messageData);
            JObject jObject = JObject.Parse(jToken.ToString());
            jToken = jObject.SelectToken("socket_id");
            if (jToken.Type == JTokenType.String)
            {
                SocketId = jToken.Value<string>();
            }

            ChangeState(ConnectionState.Connected);
            _connectionSemaphore?.Release();
            _websocket.Closed += WebsocketAutoReconnect;
            _websocket.Error += WebsocketError;
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

                    if (_websocket != null)
                    {
                        if (State != ConnectionState.Disconnected)
                        {
                            if (_pusher.IsTracingEnabled)
                            {
                                Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Attempting websocket reconnection");
                            }

                            ChangeState(ConnectionState.Connecting);
                            _websocket.MessageReceived += WebsocketMessageReceived;
                            _websocket.Closed += WebsocketAutoReconnect;
                            _websocket.Error += WebsocketError;
                            _websocket.Open();
                        }
                    }
                }
                catch (Exception error)
                {
                    RaiseError(new ReconnectionException(error));
                }
            });
        }

        private void ParseError(JToken jToken)
        {
            JToken message = jToken.SelectToken("message");
            if (message != null && message.Type == JTokenType.String)
            {
                ErrorCodes error = ErrorCodes.Unknown;
                JToken code = jToken.SelectToken("code");
                if (code != null && code.Type == JTokenType.Integer)
                {
                    if (Enum.IsDefined(typeof(ErrorCodes), code.Value<int>()))
                    {
                        error = (ErrorCodes)code.Value<int>();
                    }
                }

                RaiseError(new PusherException(message.Value<string>(), error));
            }
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;
            _pusher.ChangeConnectionState(state);
        }

        private void RaiseError(PusherException error)
        {
            _currentError = error;
            _pusher.ErrorOccured(error);
            if (_connectionSemaphore != null)
            {
                _connectionSemaphore.Release();
            }
        }
    }
}
