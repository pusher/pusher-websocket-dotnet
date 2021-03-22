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
        private const string ExpectedTextField = "Clock, hour-glass, nut\n⏰,⏳,⏣\n";
        private const int ExpectedIntegerField = 16078622;

        private readonly List<Pusher> _clients = new List<Pusher>(10);
        private Pusher _remoteClient;

        [SetUp]
        public async Task ConnectAsync()
        {
            _remoteClient = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            await _remoteClient.ConnectAsync().ConfigureAwait(false);
        }

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
            _remoteClient = null;
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
            TriggerEventException exception = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                exception = error;
            }

            // Assert
            Assert.IsNotNull(exception, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventPublicChannelError, exception.PusherCode);
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
            TriggerEventException exception = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                exception = error;
            }

            // Assert
            Assert.IsNotNull(exception, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNameInvalidError, exception.PusherCode);
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
            TriggerEventException exception = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                exception = error;
            }

            // Assert
            Assert.IsNotNull(exception, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNotConnectedError, exception.PusherCode);
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
            TriggerEventException exception = null;

            // Act
            await localPusher.UnsubscribeAllAsync().ConfigureAwait(false);
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (TriggerEventException error)
            {
                exception = error;
            }

            // Assert
            Assert.IsNotNull(exception, $"Expected a {nameof(TriggerEventException)}");
            Assert.AreEqual(ErrorCodes.TriggerEventNotSubscribedError, exception.PusherCode);
        }

        [Test]
        public async Task TriggerNullEventNameErrorTestAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Public;
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = null;
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            ArgumentNullException exception = null;

            // Act
            try
            {
                await localChannel.TriggerAsync(testEventName, pusherEvent.Data);
            }
            catch (ArgumentNullException error)
            {
                exception = error;
            }

            // Assert
            Assert.IsNotNull(exception, $"Expected a {nameof(ArgumentNullException)}");
            Assert.IsTrue(exception.Message.Contains("eventName"));
        }
    }
}
