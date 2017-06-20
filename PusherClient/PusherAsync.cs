using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PusherClient
{
    public class PusherAsync : EventEmitter, IPusher
    {
        private readonly string _applicationKey;
        private readonly PusherOptions _options;
        private ConnectionAsync _connection;

        /// <summary>
        /// The TraceSource instance to be used for logging
        /// </summary>
        public static TraceSource Trace = new TraceSource(nameof(Pusher));

        /// <summary>
        /// Initializes a new instance of the <see cref="Pusher" /> class.
        /// </summary>
        /// <param name="applicationKey">The application key.</param>
        /// <param name="options">The options.</param>
        public PusherAsync(string applicationKey, PusherOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(applicationKey))
                throw new ArgumentException(ErrorConstants.ApplicationKeyNotSet, nameof(applicationKey));

            _applicationKey = applicationKey;
            _options = options ?? new PusherOptions { Encrypted = false };
        }

        /// <summary>
        /// Gets the channels in use by the Client
        /// </summary>
        public Dictionary<string, Channel> Channels { get; } = new Dictionary<string, Channel>();

        void IPusher.ConnectionStateChanged(ConnectionState state)
        {
        }

        void IPusher.ErrorOccured(PusherException pusherException)
        {
        }

        void IPusher.EmitPusherEvent(string eventName, string data)
        {
        }

        void IPusher.EmitChannelEvent(string channelName, string eventName, string data)
        {
        }

        void IPusher.AddMember(string channelName, string member)
        {
        }

        void IPusher.RemoveMember(string channelName, string member)
        {
        }

        void IPusher.SubscriptionSuceeded(string channelName, string data)
        {
        }

        public enum AsyncConnectionState
        {
            NotConnected,
            AlreadyConnected,
            Connected,
            ConnectionFailed,
            Disconnected,
            DisconnectionFailed
        };

        public async Task<AsyncConnectionState> Connect()
        {
            // Ensure we only ever attempt to connect once
            if (_connection != null)
            {
                Trace.TraceEvent(TraceEventType.Warning, 0, ErrorConstants.ConnectionAlreadyConnected);
                return AsyncConnectionState.AlreadyConnected;
            }

            var scheme = _options.Encrypted ? Constants.SECURE_SCHEMA : Constants.INSECURE_SCHEMA;

            var url = $"{scheme}{_options.Host}/app/{_applicationKey}?protocol={Settings.Default.ProtocolVersion}&client={Settings.Default.ClientName}&version={Settings.Default.VersionNumber}";

            _connection = new ConnectionAsync(this, url);
            var connectionResult = await _connection.Connect();

            var result = connectionResult ? AsyncConnectionState.Connected : AsyncConnectionState.ConnectionFailed;

            return result;
        }

        public async Task<AsyncConnectionState> Disconnect()
        {
            if (_connection != null)
            {
                //MarkChannelsAsUnsubscribed();
                var connectionResult = await _connection.Disconnect();
                _connection = null;

                return connectionResult ? AsyncConnectionState.Disconnected : AsyncConnectionState.DisconnectionFailed;
            }

            return AsyncConnectionState.NotConnected;
        }

        //public async Task<Channel> Subscribe(string channelName)
        //{
        //    if (string.IsNullOrWhiteSpace(channelName))
        //    {
        //        throw new ArgumentException("The channel name cannot be null or whitespace", nameof(channelName));
        //    }

        //    if (AlreadySubscribed(channelName))
        //    {
        //        Trace.TraceEvent(TraceEventType.Warning, 0, "Channel '" + channelName + "' is already subscribed to. Subscription event has been ignored.");
        //        return Channels[channelName];
        //    }

        //    _pendingChannelSubscriptions.Add(channelName);

        //    return await SubscribeToChannel(channelName);
        //}

        //private async Channel SubscribeToChannel(string channelName)
        //{

        //}
    }
}
