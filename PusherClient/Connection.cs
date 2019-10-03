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
    internal class Connection
    {
        private WebSocket _websocket;

        private readonly string _url;
        private readonly IPusher _pusher;
        private bool _allowReconnect = true;
        
        private int _backOffMillis;

        private static readonly int MAX_BACKOFF_MILLIS = 10000;
        private static readonly int BACK_OFF_MILLIS_INCREMENT = 1000;

        internal string SocketId { get; private set; }

        internal ConnectionState State { get; private set; } = ConnectionState.Uninitialized;

        internal bool IsConnected => State == ConnectionState.Connected;

        private TaskCompletionSource<ConnectionState> _connectionTaskComplete = null;
        private TaskCompletionSource<ConnectionState> _disconnectionTaskComplete = null;

        public Connection(IPusher pusher, string url)
        {
            _pusher = pusher;
            _url = url;
        }

        internal Task<ConnectionState> Connect()
        {
            var completionSource = _connectionTaskComplete;
            if (completionSource != null)
                return completionSource.Task;

            completionSource = new TaskCompletionSource<ConnectionState>();
            var existingCompletionSource = Interlocked.CompareExchange(ref _connectionTaskComplete, completionSource, null);
            if (existingCompletionSource != null)
                return existingCompletionSource.Task;

            // TODO: Add 'connecting_in' event
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Connecting to: {_url}");

            ChangeState(ConnectionState.Initialized);
            _allowReconnect = true;

            _websocket = new WebSocket(_url)
            {
                EnableAutoSendPing = true,
                AutoSendPingInterval = 1
            };

            _websocket.Opened += websocket_Opened;
            _websocket.Error += websocket_Error;
            _websocket.Closed += websocket_Closed;
            _websocket.MessageReceived += websocket_MessageReceived;

            _websocket.Open();

            return completionSource.Task;
        }

        internal Task<ConnectionState> Disconnect()
        {
            var completionSource = _disconnectionTaskComplete;
            if (completionSource != null)
                return completionSource.Task;

            completionSource = new TaskCompletionSource<ConnectionState>();
            var existingCompletionSource = Interlocked.CompareExchange(ref _disconnectionTaskComplete, completionSource, null);
            if (existingCompletionSource != null)
                return existingCompletionSource.Task;

            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, $"Disconnecting from: {_url}");

            ChangeState(ConnectionState.Disconnecting);

            _allowReconnect = false;
            _websocket.Close();

            return completionSource.Task;
        }

        internal async Task<bool> Send(string message)
        {
            if (IsConnected)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Sending: " + message);
                Debug.WriteLine("Sending: " + message);

                var sendTask = Task.Run(() => _websocket.Send(message));
                await sendTask;

                return true;
            }

            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Did not send: " + message + ", as there is not active connection.");
            Debug.WriteLine("Did not send: " + message + ", as there is not active connection.");

            return false;
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

            var jsonMessage = jObject.ToString(Formatting.None);
            var template = new { @event = string.Empty, data = string.Empty, channel = string.Empty };

            var message = JsonConvert.DeserializeAnonymousType(jsonMessage, template);

            var eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);

            if (jObject["data"] != null)
                eventData["data"] = jObject["data"].ToString(Formatting.None); // undo any kind of deserialisation of the data property

            var receivedEvent = new PusherEvent(eventData, jsonMessage);

            _pusher.EmitPusherEvent(message.@event, receivedEvent);

            if (message.@event.StartsWith(Constants.PUSHER_MESSAGE_PREFIX))
            {
                // Assume Pusher event
                switch (message.@event)
                {
                    // TODO - Need to handle Error on subscribing to a channel

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
                        RaiseError(new PusherException("Error received on channel subscriptions: " + e.Message, ErrorCodes.SubscriptionError));
                        break;

                    case Constants.CHANNEL_MEMBER_ADDED:
                        _pusher.AddMember(message.channel, message.data);

                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
                        break;

                    case Constants.CHANNEL_MEMBER_REMOVED:
                        _pusher.RemoveMember(message.channel, message.data);

                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
                        break;
                }
            }
            else // Assume channel event
            {
                _pusher.EmitChannelEvent(message.channel, message.@event, receivedEvent);
            }
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Information, 0, "Websocket opened OK.");
            _connectionTaskComplete.SetResult(ConnectionState.Connected);
            _connectionTaskComplete = null;
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Websocket connection has been closed");

            _websocket.Opened -= websocket_Opened;
            _websocket.Error -= websocket_Error;
            _websocket.Closed -= websocket_Closed;
            _websocket.MessageReceived -= websocket_MessageReceived;

            if (_websocket != null)
            {
                _websocket.Dispose();
                _websocket = null;
            }

            ChangeState(ConnectionState.Disconnected);

            if (_allowReconnect)
            {
                Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Attempting websocket reconnection");

                ChangeState(ConnectionState.WaitingToReconnect);
                Task.WaitAll(Task.Delay(_backOffMillis));
                _backOffMillis = Math.Min(MAX_BACKOFF_MILLIS, _backOffMillis + BACK_OFF_MILLIS_INCREMENT);
                Connect(); // TODO
            }
            else
            {
                _disconnectionTaskComplete.SetResult(ConnectionState.Disconnected);
                _disconnectionTaskComplete = null;
            }
        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Pusher.Trace.TraceEvent(TraceEventType.Error, 0, "Error: " + e.Exception);

            if (_connectionTaskComplete != null)
            {
                _connectionTaskComplete.TrySetException(e.Exception);
            }

            if (_disconnectionTaskComplete != null)
            {
                _disconnectionTaskComplete.TrySetException(e.Exception);
            }
        }

        private void ParseConnectionEstablished(string data)
        {
            var template = new { socket_id = string.Empty };
            var message = JsonConvert.DeserializeAnonymousType(data, template);
            SocketId = message.socket_id;

            ChangeState(ConnectionState.Connected);
        }

        private void ParseError(string data)
        {
            var template = new { message = string.Empty, code = (int?) null };
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
            _pusher.ErrorOccured(error);
        }
    }
}
