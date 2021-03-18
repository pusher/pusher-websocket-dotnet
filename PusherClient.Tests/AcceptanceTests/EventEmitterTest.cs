using NUnit.Framework;
using System;
using PusherClient.Tests.Utilities;
using System.Threading.Tasks;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public partial class EventEmitterTest
    {
        private Pusher _pusher;

        [SetUp]
        public async Task ConnectAsync()
        {
            _pusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence);
            _pusher.Error += HandlePusherError;
            await _pusher.ConnectAsync().ConfigureAwait(false);
        }

        [TearDown]
        public async Task DestroyAsync()
        {
            await PusherFactory.DisposePusherAsync(_pusher).ConfigureAwait(false);
            _pusher = null;
        }

        private void HandlePusherError(object sender, PusherException error)
        {
            System.Diagnostics.Trace.TraceError($"Pusher error detected on socket {_pusher.SocketID}:{Environment.NewLine}{error}");
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
