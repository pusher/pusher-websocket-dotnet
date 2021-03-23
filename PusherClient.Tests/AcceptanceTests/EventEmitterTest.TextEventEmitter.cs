using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PusherClient.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests for <see cref="TextEventEmitter"/>.
    /// </summary>
    public partial class EventEmitterTest
    {
        #region Presence channels

        [Test]
        public async Task TextEventEmitterPresenceChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await TextEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task TextEventEmitterPresenceChannelUnbindListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await TextEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 2 }).ConfigureAwait(false);
        }

        [Test]
        public async Task TextEventEmitterPresenceChannelUnbindGeneralListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await TextEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0 }).ConfigureAwait(false);
        }

        #endregion

        #region Private channels

        [Test]
        public async Task TextEventEmitterPrivateChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await TextEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task TextEventEmitterPrivateChannelUnbindAllListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await TextEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 2, 3 }).ConfigureAwait(false);
        }

        [Test]
        public async Task TextEventEmitterPrivateChannelUnbindAllGeneralListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await TextEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1 }).ConfigureAwait(false);
        }

        [Test]
        public async Task TextEventEmitterPrivateChannelUnbindAllTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await TextEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1, 2, 3 }).ConfigureAwait(false);
        }

        #endregion

        #region Test helper functions

        private static string CreateTextEventData()
        {
            EventTestData data = new EventTestData
            {
                TextField = ExpectedTextField,
                IntegerField = ExpectedIntegerField,
            };
            return JsonConvert.SerializeObject(data);
        }

        private static void ValidateTextEvent(string channelName, string eventName, string textEvent)
        {
            string token = channelName;
            Assert.IsTrue(textEvent.Contains(token), $"Token not found '{token}'");

            token = eventName;
            Assert.IsTrue(textEvent.Contains(token), $"Token not found '{token}'");

            // Encode the line feed character because it is encoded in a Json string field in textEvent.
            token = ExpectedTextField.Replace("\n", "\\\\n");
            Assert.IsTrue(textEvent.Contains(token), $"Token not found '{token}'");

            token = ExpectedIntegerField.ToString();
            Assert.IsTrue(textEvent.Contains(token), $"Token not found '{token}'");

            if (Channel.GetChannelType(channelName) == ChannelTypes.Presence)
            {
                token = "\"user_id\"";
                Assert.IsTrue(textEvent.Contains(token), $"Token not found '{token}'");
            }
        }

        private async Task TextEventEmitterTestAsync(ChannelTypes channelType)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-text-event-test";
            AutoResetEvent globalEventReceived = new AutoResetEvent(false);
            AutoResetEvent channelEventReceived = new AutoResetEvent(false);
            string globalEvent = null;
            string channelEvent = null;
            string textEventData = CreateTextEventData();
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(channelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(channelName).ConfigureAwait(false);

            void GeneralListener(string eventName, string eventData)
            {
                if (eventName == testEventName)
                {
                    globalEvent = eventData;
                    globalEventReceived.Set();
                }
            }

            void Listener(string eventData)
            {
                channelEvent = eventData;
                channelEventReceived.Set();
            }

            // Act
            localPusher.BindAll(GeneralListener);
            localChannel.Bind(testEventName, Listener);
            remoteChannel.Trigger(testEventName, textEventData);

            // Assert
            Assert.IsTrue(globalEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(channelEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            ValidateTextEvent(channelName, testEventName, globalEvent);
            ValidateTextEvent(channelName, testEventName, channelEvent);
        }

        private async Task TextEventEmitterUnbindTestAsync(ChannelTypes channelType, IList<int> listenersToUnbind)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            string textEventData = CreateTextEventData();
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

            void Listener(int index, string eventData)
            {
                string eventName = null;
                JObject jObject = JObject.Parse(eventData);
                JToken jToken = jObject.SelectToken("event");
                if (jToken != null && jToken.Type == JTokenType.String)
                {
                    eventName = jToken.Value<string>();
                }

                if (eventName == testEventNames[index])
                {
                    numberEventsReceived[index]++;
                    if (eventExpected[index])
                    {
                        receivedEvents[index].Set();
                    }
                }
            }

            void GeneralListener0(string eventName, string eventData)
            {
                Listener(0, eventData);
            }

            void GeneralListener1(string eventName, string eventData)
            {
                Listener(1, eventData);
            }

            void Listener2(string eventData)
            {
                Listener(2, eventData);
            }

            void Listener3(string eventData)
            {
                Listener(3, eventData);
            }

            localPusher.BindAll(GeneralListener0);
            localPusher.BindAll(GeneralListener1);
            localChannel.Bind(testEventName, Listener2);
            localChannel.Bind(testEventName, Listener3);
            await remoteChannel.TriggerAsync(testEventName, textEventData).ConfigureAwait(false);
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

            await remoteChannel.TriggerAsync(testEventName, textEventData).ConfigureAwait(false);
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
