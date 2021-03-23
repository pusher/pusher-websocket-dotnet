using Newtonsoft.Json;
using NUnit.Framework;
using PusherClient.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests for <see cref="DynamicEventEmitter"/>.
    /// </summary>
    public partial class EventEmitterTest
    {
        #region Presence channels

        [Test]
        public async Task DynamicEventEmitterPresenceChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await DynamicEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task DynamicEventEmitterPresenceChannelUnbindListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await DynamicEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 3 }).ConfigureAwait(false);
        }

        [Test]
        public async Task DynamicEventEmitterPresenceChannelUnbindGeneralListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await DynamicEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0 }).ConfigureAwait(false);
        }

        #endregion

        #region Private channels

        [Test]
        public async Task DynamicEventEmitterPrivateChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await DynamicEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task DynamicEventEmitterPrivateChannelUnbindAllListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await DynamicEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 2, 3 }).ConfigureAwait(false);
        }

        [Test]
        public async Task DynamicEventEmitterPrivateChannelUnbindAllGeneralListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await DynamicEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1 }).ConfigureAwait(false);
        }

        [Test]
        public async Task DynamicEventEmitterPrivateChannelUnbindAllTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await DynamicEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1, 2, 3 }).ConfigureAwait(false);
        }

        #endregion

        #region Test helper functions

        private static dynamic CreateDynamicEventData()
        {
            dynamic data = new
            {
                TextField = ExpectedTextField,
                IntegerField = ExpectedIntegerField,
            };
            return data;
        }

        private static void ValidateDynamicEvent(string channelName, string eventName, dynamic dynamicEvent)
        {
            Assert.AreEqual(channelName, dynamicEvent.channel, "channel");
            Assert.AreEqual(eventName, dynamicEvent.@event, "event");
            if (Channel.GetChannelType(channelName) == ChannelTypes.Presence)
            {
                Assert.IsNotNull(dynamicEvent.user_id, "user_id");
            }

            EventTestData actual = JsonConvert.DeserializeObject<EventTestData>(dynamicEvent.data.ToString());
            Assert.AreEqual(ExpectedIntegerField, actual.IntegerField, nameof(actual.IntegerField));
            Assert.IsNull(actual.NothingField, nameof(actual.NothingField));
            Assert.AreEqual(ExpectedTextField, actual.TextField, nameof(actual.TextField));
        }

        private async Task DynamicEventEmitterTestAsync(ChannelTypes channelType)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-dynamic-event-test";
            AutoResetEvent globalEventReceived = new AutoResetEvent(false);
            AutoResetEvent channelEventReceived = new AutoResetEvent(false);
            dynamic globalEvent = null;
            dynamic channelEvent = null;
            dynamic dynamicEventData = CreateDynamicEventData();
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(channelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(channelName).ConfigureAwait(false);

            void GeneralListener(string eventName, dynamic eventData)
            {
                if (eventName == testEventName)
                {
                    globalEvent = eventData;
                    globalEventReceived.Set();
                }
            }

            void Listener(dynamic eventData)
            {
                channelEvent = eventData;
                channelEventReceived.Set();
            }

            // Act
            localPusher.BindAll(GeneralListener);
            localChannel.Bind(testEventName, Listener);
            remoteChannel.Trigger(testEventName, dynamicEventData);

            // Assert
            Assert.IsTrue(globalEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(channelEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            ValidateDynamicEvent(channelName, testEventName, globalEvent);
            ValidateDynamicEvent(channelName, testEventName, channelEvent);
        }

        private async Task DynamicEventEmitterUnbindTestAsync(ChannelTypes channelType, IList<int> listenersToUnbind)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-dynamic-event-test";
            dynamic dynamicEventData = CreateDynamicEventData();
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            string[] testEventNames = new string[] { testEventName, testEventName, testEventName, testEventName, };
            AutoResetEvent[] receivedEvents = new AutoResetEvent[testEventNames.Length];
            int[] numberEventsReceived = new int[testEventNames.Length];
            int[] totalEventsExpected = new int[testEventNames.Length];
            bool[] eventExpected = new bool[testEventNames.Length];
            for (int i = 0; i < testEventNames.Length; i++)
            {
                receivedEvents[i] = new AutoResetEvent(false);
                numberEventsReceived[i] = 0;
                eventExpected[i] = true;
                if (listenersToUnbind.Contains(i)) totalEventsExpected[i] = 1;
                else totalEventsExpected[i] = 2;
            }

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(channelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(channelName).ConfigureAwait(false);

            void Listener(int index, dynamic eventData)
            {
                string eventName = eventData.@event;
                if (eventName == testEventNames[index])
                {
                    numberEventsReceived[index]++;
                    if (eventExpected[index])
                    {
                        receivedEvents[index].Set();
                    }
                }
            }

            void GeneralListener0(string eventName, dynamic eventData)
            {
                Listener(0, eventData);
            }

            void GeneralListener1(string eventName, dynamic eventData)
            {
                Listener(1, eventData);
            }

            void Listener2(dynamic eventData)
            {
                Listener(2, eventData);
            }

            void Listener3(dynamic eventData)
            {
                Listener(3, eventData);
            }

            localPusher.BindAll(GeneralListener0);
            localPusher.BindAll(GeneralListener1);
            localChannel.Bind(testEventName, Listener2);
            localChannel.Bind(testEventName, Listener3);
            await remoteChannel.TriggerAsync(testEventName, dynamicEventData).ConfigureAwait(false);
            for (int i = 0; i < testEventNames.Length; i++)
            {
                Assert.IsTrue(receivedEvents[i].WaitOne(TimeSpan.FromSeconds(5)), $"receivedEvents[{i}]");
                receivedEvents[i].Reset();
            }

            TimeSpan delayAfterTrigger = TimeSpan.FromMilliseconds(0);
            foreach (int index in listenersToUnbind)
            {
                eventExpected[index] = false;
            }

            // Act
            if (listenersToUnbind.Count == testEventNames.Length)
            {
                // Not expecting any events, so wait a bit and ensure that none come in.
                delayAfterTrigger = TimeSpan.FromMilliseconds(500);
                localPusher.UnbindAll();
                localChannel.UnbindAll();
            }
            else
            {
                if (listenersToUnbind.Contains(0)) localPusher.Unbind(GeneralListener0);
                if (listenersToUnbind.Contains(1)) localPusher.Unbind(GeneralListener1);
                if (listenersToUnbind.Contains(2) && listenersToUnbind.Contains(3)) localChannel.Unbind(testEventName);
                else
                {
                    if (listenersToUnbind.Contains(2)) localChannel.Unbind(testEventName, Listener2);
                    if (listenersToUnbind.Contains(3)) localChannel.Unbind(testEventName, Listener3);
                }
            }

            await remoteChannel.TriggerAsync(testEventName, dynamicEventData).ConfigureAwait(false);
            await Task.Delay(delayAfterTrigger).ConfigureAwait(false);

            // Assert
            for (int i = 0; i < testEventNames.Length; i++)
            {
                if (eventExpected[i])
                {
                    Assert.IsTrue(receivedEvents[i].WaitOne(TimeSpan.FromSeconds(5)), $"receivedEvents[{i}]");
                }

                Assert.AreEqual(totalEventsExpected[i], numberEventsReceived[i], $"# Event[{i}]");
            }
        }

        #endregion
    }
}
