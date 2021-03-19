using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using PusherClient.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests for <see cref="PusherEventEmitter"/>.
    /// </summary>
    public partial class EventEmitterTest
    {
        [Test]
        public async Task PusherEventEmitterPresenceChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPrivateChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await PusherEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        private static PusherEvent CreatePusherEvent(ChannelTypes channelType, string eventName)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                { nameof(RawPusherEvent.channel), ChannelNameFactory.CreateUniqueChannelName(channelType: channelType) },
                { nameof(RawPusherEvent.@event), eventName },
                { nameof(RawPusherEvent.data), "Date: " + DateTime.Now.ToString("o") },
            };

            PusherEvent result = new PusherEvent(properties, JsonConvert.SerializeObject(properties));
            return result;
        }

        private static void AssertPusherEventsAreEqual(ChannelTypes channelType, PusherEvent expected, PusherEvent actual)
        {
            Assert.IsNotNull(expected, nameof(expected));
            Assert.IsNotNull(actual, nameof(actual));

            if (channelType == ChannelTypes.Presence)
            {
                Assert.IsNull(expected.UserId);
                Assert.IsNotNull(actual.UserId);
            }
            else
            {
                Assert.IsNull(expected.UserId);
                Assert.IsNull(actual.UserId);
            }

            Assert.IsNotNull(actual.ChannelName);
            Assert.AreEqual(expected.ChannelName, actual.ChannelName);

            Assert.IsNotNull(actual.EventName);
            Assert.AreEqual(expected.EventName, actual.EventName);

            Assert.IsNotNull(actual.Data);
            Assert.AreEqual(expected.Data, actual.Data);
        }

        private async Task PusherEventEmitterTestAsync(ChannelTypes channelType)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            localPusher.Error += HandlePusherError;
            string testEventName = "client-pusher-event-test";
            AutoResetEvent globalEventReceived = new AutoResetEvent(false);
            AutoResetEvent channelEventReceived = new AutoResetEvent(false);
            PusherEvent globalEvent = null;
            PusherEvent channelEvent = null;
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);

            // Act
            localPusher.BindAll((string eventName, PusherEvent eventData) =>
            {
                if (eventName == testEventName)
                {
                    globalEvent = eventData;
                    globalEventReceived.Set();
                }
            });

            localChannel.Bind(testEventName, (PusherEvent eventData) =>
            {
                channelEvent = eventData;
                channelEventReceived.Set();
            });

            remoteChannel.Trigger(testEventName, pusherEvent.Data);

            // Assert
            Assert.IsTrue(globalEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(channelEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            AssertPusherEventsAreEqual(channelType, pusherEvent, globalEvent);
            AssertPusherEventsAreEqual(channelType, pusherEvent, channelEvent);
        }
    }
}
