using System;
using System.Collections.Generic;
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
        private readonly IChannelDataDecrypter _dataDecrypter = new ChannelDataDecrypter();

        private int _backOffMillis;

        private static readonly int MAX_BACKOFF_MILLIS = 10000;
        private static readonly int BACK_OFF_MILLIS_INCREMENT = 1000;

        private SemaphoreSlim _connectionSemaphore;
        private Exception _currentError;
        private bool _autoReconnecting;

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
                if (_pusher.PusherOptions.TraceLogger != null)
                {
                    _pusher.PusherOptions.TraceLogger.TraceInformation($"Connecting to: {_url}");
                }

                ChangeState(ConnectionState.Connecting);

                CreateNewWebSocket();

                await Task.Run(() =>
                {
                    _websocket.Error += WebsocketConnectionError;
                    _websocket.MessageReceived += WebsocketMessageReceived;
                    _websocket.Open();
                }).ConfigureAwait(false);

                TimeSpan timeoutPeriod = _pusher.PusherOptions.InnerClientTimeout;
                if (!await _connectionSemaphore.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, Constants.CONNECTION_ESTABLISHED);
                }

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
                    ChangeState(ConnectionState.Disconnecting);

                    if (_pusher.PusherOptions.TraceLogger != null)
                    {
                        _pusher.PusherOptions.TraceLogger.TraceInformation($"Disconnecting from: {_url}");
                    }

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
                if (_pusher.PusherOptions.TraceLogger != null)
                {
                    _pusher.PusherOptions.TraceLogger.TraceInformation($"Sending:{Environment.NewLine}{message}");
                }

                await Task.Run(() => _websocket.Send(message)).ConfigureAwait(false);
                return true;
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
                        // If the message is not a string we need the raw Json as a string
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
                    string textEvent = jsonMessage;
                    if (textEvent != null)
                    {
                        textEventEmitter.EmitEvent(eventName, textEvent);
                    }
                }
                else if (binder is DynamicEventEmitter dynamicEventEmitter)
                {
                    var template = new { @event = string.Empty, data = string.Empty, channel = string.Empty, user_id = string.Empty };
                    dynamic dynamicEvent = JsonConvert.DeserializeAnonymousType(jsonMessage, template);
                    if (dynamicEvent != null)
                    {
                        dynamicEventEmitter.EmitEvent(eventName, dynamicEvent);
                    }
                }
            }
        }

        private void EmitChannelEvent(string eventName, string jsonMessage, string channelName, Dictionary<string, object> message)
        {
            foreach (string key in EventEmitter.EmitterKeys)
            {
                IEventBinder binder = _pusher.GetChannelEventBinder(key, channelName);
                EmitEvent(eventName, binder, jsonMessage, message);
            }
        }

        private void EmitEvent(string eventName, string jsonMessage, Dictionary<string, object> message)
        {
            foreach (string key in EventEmitter.EmitterKeys)
            {
                IEventBinder binder = _pusher.GetEventBinder(key);
                EmitEvent(eventName, binder, jsonMessage, message);
            }
        }

        private void ProcessPusherEvent(string eventName, string rawJson, Dictionary<string, object> message)
        {
            string messageData = string.Empty;
            if (message.ContainsKey("data"))
            {
                messageData = (string)message["data"];
            }
            
            switch (eventName)
            {
                case Constants.CONNECTION_ESTABLISHED:
                    ParseConnectionEstablished(messageData);
                    break;
                    
                case Constants.PUSHER_SIGNIN_SUCCESS:
                case Constants.PUSHER_WATCHLIST_EVENT:
                    EmitEvent(eventName, rawJson, message);
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
                
                case Constants.CHANNEL_SUBSCRIPTION_COUNT:
                    _pusher.SubscriberCount(channelName, messageData);
                    break;

                default:
                    processed = false;
                    break;
            }

            return processed;
        }

        private void WebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string eventName = null;
            string channelName = null;
            try
            {
                if (_pusher.PusherOptions.TraceLogger != null)
                {
                    _pusher.PusherOptions.TraceLogger.TraceInformation($"Websocket message received:{Environment.NewLine}{e.Message}");
                }

                JObject jObject = JObject.Parse(e.Message);
                string rawJson = jObject.ToString(Formatting.None);
                Dictionary<string, object> message = GetEventPropertiesFromMessage(rawJson);
                if (message.ContainsKey("event"))
                {
                    eventName = (string)message["event"];

                    if (eventName == Constants.ERROR)
                    {
                        /*
                         *  Errors are in a different Json form to other messages.
                         *  The data property is an object and not a string and needs to be dealt with differently; for example:
                         *  {"event":"pusher:error","data":{"code":4001,"message":"App key Invalid not in this cluster. Did you forget to specify the cluster?"}}
                         */
                        ParseError(jObject["data"]);
                    }
                    else
                    {
                        /*
                         *  For messages other than "pusher:error" the data property is a string; for example:
                         *  {
                         *    "event": "pusher:connection_established",
                         *    "data": "{\"socket_id\":\"131160.155806628\"}"
                         *  }
                         *  
                         *  {
                         *    "event": "pusher_internal:subscription_succeeded",
                         *    "data": "{\"presence\":{\"count\":1,\"ids\":[\"131160.155806628\"],\"hash\":{\"131160.155806628\":{\"name\":\"user-1\"}}}}",
                         *    "channel": "presence-channel-1"
                         *  }
                         */
                        string messageData = string.Empty;
                        if (message.ContainsKey("data"))
                        {
                            messageData = (string)message["data"];
                        }

                        if (message.ContainsKey("channel"))
                        {
                            channelName = (string)message["channel"];
                            if (!ProcessPusherChannelEvent(eventName, channelName, messageData))
                            {
                                byte[] decryptionKey = _pusher.GetSharedSecret(channelName);
                                if (decryptionKey != null)
                                {
                                    message["data"] = _dataDecrypter.DecryptData(decryptionKey, EncryptedChannelData.CreateFromJson(messageData));
                                }

                                EmitEvent(eventName, rawJson, message);
                                EmitChannelEvent(eventName, rawJson, channelName, message);
                            }
                        }
                        else
                        {
                            ProcessPusherEvent(eventName, rawJson, message);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                string operation = nameof(WebsocketMessageReceived);
                PusherException error;
                if (channelName != null)
                {
                    if (exception is ChannelException channelException)
                    {
                        channelException.ChannelName = channelName;
                        channelException.EventName = eventName;
                        channelException.SocketID = SocketId;
                        error = channelException;
                    }
                    else
                    {
                        error = new ChannelException($"An unexpected error was detected when performing the operation '{operation}'", ErrorCodes.MessageReceivedError, channelName, SocketId, exception)
                        {
                            EventName = eventName,
                        };
                    }
                }
                else
                {
                    if (eventName != null)
                    {
                        operation += $" for event '{eventName}'";
                    }

                    error = new OperationException(ErrorCodes.MessageReceivedError, operation, exception);
                }

                RaiseError(error);
            }
        }

        private void WebsocketConnectionError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _currentError = e.Exception;
            _connectionSemaphore?.Release();
            WebsocketError(sender, e);
        }

        private void WebsocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (_pusher.PusherOptions.TraceLogger != null)
            {
                _pusher.PusherOptions.TraceLogger.TraceError($"Websocket error:{Environment.NewLine}{e.Exception}");
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
            if (_autoReconnecting)
            {
                return;
            }

            try
            {
                _autoReconnecting = true;
                RecreateWebSocket();
            }
            catch
            {
                _autoReconnecting = false;
                throw;
            }

            Task.Run(() =>
            {
                try
                {
                    _backOffMillis = Math.Min(MAX_BACKOFF_MILLIS, _backOffMillis + BACK_OFF_MILLIS_INCREMENT);
                    if (_pusher.PusherOptions.TraceLogger != null)
                    {
                        _pusher.PusherOptions.TraceLogger.TraceWarning($"Waiting {_backOffMillis} ms before attempting to reconnect");
                    }

                    ChangeState(ConnectionState.WaitingToReconnect);
                    Task.WaitAll(Task.Delay(_backOffMillis));

                    if (_websocket != null)
                    {
                        if (State != ConnectionState.Disconnected)
                        {
                            if (_pusher.PusherOptions.TraceLogger != null)
                            {
                                _pusher.PusherOptions.TraceLogger.TraceWarning("Attempting websocket reconnection");
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
                    RaiseError(new OperationException(ErrorCodes.ReconnectError, nameof(WebsocketAutoReconnect), error));
                }
                finally
                {
                    _autoReconnecting = false;
                }
            });
        }

        private void DisposeWebsocket()
        {
            _currentError = null;
            _websocket.MessageReceived -= WebsocketMessageReceived;
            _websocket.Closed -= WebsocketAutoReconnect;
            _websocket.Error -= WebsocketError;
            _websocket.Dispose();
            _websocket = null;
            ChangeState(ConnectionState.Disconnected);
        }

        private void CreateNewWebSocket()
        {
            _websocket = new WebSocket(_url,
                                        "",                     //string subProtocol = "",
                                        null,                   //List<KeyValuePair<string, string>> cookies = null,
                                        null,                   //List<KeyValuePair<string, string>> customHeaderItems = null,
                                        "",                     //string userAgent = "",
                                        "",                     //string origin = "",
                                        WebSocketVersion.None,  // WebSocketVersion version = WebSocketVersion.None,
                                        null,                   // EndPoint httpConnectProxy = null,
                                        _pusher.PusherOptions.EnabledSslProtocols,
                                        0)                      // int receiveBufferSize = 0)
            {
                EnableAutoSendPing = true,
                AutoSendPingInterval = 1
            };
        }


        private void RecreateWebSocket()
        {
            DisposeWebsocket();
            CreateNewWebSocket();
        }

        private void ParseError(JToken jToken)
        {
            JToken message = jToken.SelectToken("message");
            if (message != null)
            {
                if (message.Type == JTokenType.String)
                {
                    ErrorCodes error = ErrorCodes.Unknown;
                    JToken code = jToken.SelectToken("code");
                    if (code != null)
                    {
                        if (code.Type == JTokenType.Integer)
                        {
                            if (Enum.IsDefined(typeof(ErrorCodes), code.Value<int>()))
                            {
                                error = (ErrorCodes)code.Value<int>();
                            }
                        }
                    }

                    RaiseError(new PusherException(message.Value<string>(), error));
                }
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
