﻿using PusherServer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PusherClient.Tests.Utilities
{
    public class FakeChannelAuthoriser : IChannelAuthorizer, IChannelAuthorizerAsync
    {
        /// <summary>
        /// The minimum latency measured in milli-seconds.
        /// </summary>
        public const int MinLatency = 300;

        /// <summary>
        /// The maximum latency measured in milli-seconds.
        /// </summary>
        public const int MaxLatency = 1000;

        public const string TamperToken = "-tamper";

        private readonly string _userName;

        private readonly byte[] _encryptionKey;

        public FakeChannelAuthoriser()
            : this("Unknown")
        {
        }

        public FakeChannelAuthoriser(string userName)
            : this(userName, null)
        {
        }

        public FakeChannelAuthoriser(string userName, byte[] encryptionKey)
        {
            _userName = userName;
            _encryptionKey = encryptionKey;
        }

        public TimeSpan? Timeout { get; set; }

        public string Authorize(string channelName, string socketId)
        {
            return AuthorizeAsync(channelName, socketId).Result;
        }

        public async Task<string> AuthorizeAsync(string channelName, string socketId)
        {
            string authData = null;
            double delay = (await LatencyInducer.InduceLatencyAsync(MinLatency, MaxLatency)) / 1000.0;
            Trace.TraceInformation($"{this.GetType().Name} paused for {Math.Round(delay, 3)} second(s)");
            await Task.Run(() =>
            {
                PusherServer.PusherOptions options = new PusherServer.PusherOptions
                {
                    EncryptionMasterKey = _encryptionKey,
                    Cluster = Config.Cluster,
                };
                var provider = new PusherServer.Pusher(Config.AppId, Config.AppKey, Config.AppSecret, options);
                if (channelName.StartsWith("presence-"))
                {
                    var channelData = new PresenceChannelData
                    {
                        user_id = socketId,
                        user_info = new FakeUserInfo { name = _userName }
                    };

                    authData = provider.Authenticate(channelName, socketId, channelData).ToJson();
                }
                else
                {
                    authData = provider.Authenticate(channelName, socketId).ToJson();
                }

                if (channelName.Contains(TamperToken))
                {
                    authData = authData.Replace("1", "2");
                }
            }).ConfigureAwait(false);
            return authData;
        }

        private static ILatencyInducer LatencyInducer { get; } = new LatencyInducer();
    }
}
