using System;
using System.Collections.Generic;
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
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence),
            };

            // Act and Assert
            await SubscribeThenConnectMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectAllChannelsThenDisconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence),
            };

            // Act
            await SubscribeThenConnectMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Need to delay as there is no channel disconnected event to wait upon.
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            AssertIsDisconnected(pusher, channelNames);
        }

        [Test]
        public async Task SubscribeThenConnectAllChannelsThenDisconnectThenReconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            var subscribedEvent = new AutoResetEvent(false);
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private),
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence),
            };

            // Act
            await SubscribeThenConnectMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Need to delay as there is no channel disconnected event to wait upon.
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            AssertIsDisconnected(pusher, channelNames);

            // Act
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == channelNames.Count)
                {
                    subscribedEvent.Set();
                }
            };
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            AssertIsSubscribed(pusher, channelNames);
        }

        [Test]
        public async Task SubscribeThenConnectUnauthorizedChannelsAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeUnauthoriser());
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + "-unauth",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + "-unauth",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
            };

            int expectedErrorCount = 0;
            foreach (string name in channelNames)
            {
                if (Channel.GetChannelType(name) != ChannelTypes.Public)
                {
                    expectedErrorCount++;
                }
            }

            pusher.Error += (sender, error) =>
            {
                if (error is ChannelUnauthorizedException)
                {
                    errorCount++;
                    if (errorCount == expectedErrorCount)
                    {
                        errorEvent.Set();
                    }
                }
            };

            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.ChannelType == ChannelTypes.Public)
                {
                    subscribedEvent.Set();
                }
            };

            // Act
            foreach (string channelName in channelNames)
            {
                await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
            }

            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors expected");
            AssertUnauthorized(pusher, channelNames);
        }

        #endregion

        #region Private static methods

        private static async Task SubscribeThenConnectTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: false, channelType, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private static async Task SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: false, channelType).ConfigureAwait(false);
        }

        private static async Task SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelMultipleTimesTestAsync(connectBeforeSubscribing: false, channelType).ConfigureAwait(false);
        }

        private static async Task SubscribeThenConnectMultipleChannelsTestAsync(Pusher pusher, IList<string> channelNames)
        {
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: false, pusher, channelNames).ConfigureAwait(false);
        }

        #endregion
    }
}
