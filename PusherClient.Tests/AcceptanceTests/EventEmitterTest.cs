using NUnit.Framework;
using System;
using PusherClient.Tests.Utilities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public partial class EventEmitterTest
    {
        private readonly List<Pusher> _clients = new List<Pusher>(10);
        private Pusher _remoteClient;

        [SetUp]
        public async Task ConnectAsync()
        {
            _remoteClient = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            _remoteClient.Error += HandlePusherError;
            await _remoteClient.ConnectAsync().ConfigureAwait(false);
        }

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
            _remoteClient = null;
        }

        private void HandlePusherError(object sender, PusherException error)
        {
            System.Diagnostics.Trace.TraceError($"Pusher error detected on socket {_remoteClient.SocketID}:{Environment.NewLine}{error}");
        }

        private class RawPusherEvent
        {
            public string user_id { get; set; }
            public string channel { get; set; }
            public string @event { get; set; }
            public string data { get; set; }
        }
    }
}
