using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    public partial class SubscriptionTest
    {
        #region Public channel tests

        [Test]
        public async Task SubscribeThenConnectPublicChannelAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPublicChannelWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Public, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePublicChannelTwiceAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePublicChannelMultipleTimesAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        #endregion

        #region Private channel tests

        [Test]
        public async Task SubscribeThenConnectPrivateChannelAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPrivateChannelWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Private, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePrivateChannelTwiceAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePrivateChannelMultipleTimesAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        #endregion

        #region Presence channel tests

        [Test]
        public async Task SubscribeThenConnectPresenceChannelAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPresenceChannelWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Presence, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePresenceChannelTwiceAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSamePresenceChannelMultipleTimesAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        #endregion

        #region Combination tests

        [Test]
        public async Task SubscribeThenConnectAllChannelsAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            var subscribedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == 3)
                {
                    subscribedEvent.Set();
                }
            };

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName();
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private);
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);

            // Act
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);
        }

        [Test]
        public async Task SubscribeThenConnectAllChannelsThenDisconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            var subscribedEvent = new AutoResetEvent(false);
            var disconnectedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == 3)
                {
                    subscribedEvent.Set();
                }
            };

            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public);
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private);
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);

            // Act
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);

            // Act
            await pusher.DisconnectAsync().ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Need to delay as there is no channel disconnected event to wait upon.
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            ValidateDisconnectedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateDisconnectedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateDisconnectedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);
        }

        [Test]
        public async Task SubscribeThenConnectAllChannelsThenDisconnectThenReconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            var subscribedEvent = new AutoResetEvent(false);
            var disconnectedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == 3)
                {
                    subscribedEvent.Set();
                }
            };

            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public);
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private);
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);

            // Act
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);

            // Act
            await pusher.DisconnectAsync().ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Need to delay as there is no channel disconnected event to wait upon.
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            ValidateDisconnectedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateDisconnectedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateDisconnectedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);

            // Act
            subscribedCount = 0;
            subscribedEvent.Reset();
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);
        }

        #endregion

        #region Private static methods

        private static async Task SubscribeThenConnectTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            // Arrange
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            AutoResetEvent[] errorEvent = { null, null };
            string mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            if (pusher == null)
            {
                pusher = PusherFactory.GetPusher(channelType: channelType);
            }

            bool[] channelSubscribed = { false, false };
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    channelSubscribed[0] = true;
                    subscribedEvent.Set();
                    if (raiseSubscribedError)
                    {
                        throw new InvalidOperationException($"Simulated error for {nameof(Pusher)}.{nameof(Pusher.Subscribed)} {channelName}.");
                    }
                }
            };

            SubscribedDelegateException[] errors = { null, null };
            if (raiseSubscribedError)
            {
                errorEvent[0] = new AutoResetEvent(false);
                errorEvent[1] = new AutoResetEvent(false);
                pusher.Error += (sender, error) =>
                {
                    if (error.ToString().Contains($"{nameof(Pusher)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[0] = error as SubscribedDelegateException;
                        errorEvent[0].Set();
                    }
                    else if (error.ToString().Contains($"{nameof(Channel)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[1] = error as SubscribedDelegateException;
                        errorEvent[1].Set();
                    }
                };
            }

            SubscriptionEventHandler subscribedEventHandler = (sender) =>
            {
                channelSubscribed[1] = true;
                if (raiseSubscribedError)
                {
                    throw new InvalidOperationException($"Simulated error for {nameof(Channel)}.{nameof(Pusher.Subscribed)} {mockChannelName}.");
                }
            };

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[0]?.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[1]?.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, channelType);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private static async Task SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType);
            var numberOfCalls = 0;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    numberOfCalls++;
                    channelSubscribed = true;
                }
            };

            // Act
            var firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            var secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            
            // Delay for enough time to be sure numberOfCalls is not greater than one
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        private static async Task SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType);
            var numberOfCalls = 0;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    numberOfCalls++;
                    channelSubscribed = true;
                }
            };

            // Act
            for (int i = 0; i < 4; i++)
            {
                await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Delay for enough time to be sure numberOfCalls is not greater than one
            await Task.Delay(1500).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        #endregion
    }
}
