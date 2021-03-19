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

        [Test]
        public async Task TriggerPublicChannelErrorTestAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Public;
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            TriggerEventException expectedException = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                expectedException = error;
            }

            // Assert
            Assert.IsNotNull(expectedException, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventPublicChannelError, expectedException.PusherCode);
        }

        [Test]
        public async Task TriggerInvalidEventNameErrorTestAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Private;
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "pusher-event-test";
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            TriggerEventException expectedException = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                expectedException = error;
            }

            // Assert
            Assert.IsNotNull(expectedException, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNameInvalidError, expectedException.PusherCode);
        }

        [Test]
        public async Task TriggerNotConnectedErrorTestAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Private;
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            TriggerEventException expectedException = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                expectedException = error;
            }

            // Assert
            Assert.IsNotNull(expectedException, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNotConnectedError, expectedException.PusherCode);
        }

        [Test]
        public async Task TriggerNotSubscribedErrorTestAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Private;
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            TriggerEventException expectedException = null;

            // Act
            await localPusher.UnsubscribeAllAsync().ConfigureAwait(false);
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                expectedException = error;
            }

            // Assert
            Assert.IsNotNull(expectedException, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNotSubscribedError, expectedException.PusherCode);
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
