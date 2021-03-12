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
        public async Task ConnectThenSubscribePublicChannelAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePublicChannelWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Public, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePublicChannelTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePublicChannelMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePublicChannelWithoutAnyEventHandlersAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
        }

        #endregion

        #region Private channel tests

        [Test]
        public async Task ConnectThenSubscribePrivateChannelAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePrivateChannelWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Private, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePrivateChannelTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePrivateChannelMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePrivateChannelWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            PusherException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeTestAsync(ChannelTypes.Private, pusher: pusher).ConfigureAwait(false);
            }
            catch (PusherException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An Authorizer needs to be provided when subscribing to a private or presence channel.", caughtException.Message);
        }

        #endregion

        #region Presence channel tests

        [Test]
        public async Task ConnectThenSubscribePresenceChannelAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePresenceChannelWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Presence, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePresenceChannelTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSamePresenceChannelMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePresenceChannelWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            PusherException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeTestAsync(ChannelTypes.Presence, pusher: pusher).ConfigureAwait(false);
            }
            catch (PusherException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An Authorizer needs to be provided when subscribing to a private or presence channel.", caughtException.Message);
        }

        #endregion

        #region Combination tests

        [Test]
        public async Task ConnectThenSubscribeAllChannelsAsync()
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
            await ConnectThenSubscribeMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeAllChannelsThenDisconnectAsync()
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
            await ConnectThenSubscribeMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Need to delay as there is no channel disconnected event to wait upon.
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            AssertIsDisconnected(pusher, channelNames);
        }

        [Test]
        public async Task ConnectThenSubscribeAllChannelsThenDisconnectThenReconnectAsync()
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
            await ConnectThenSubscribeMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
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
        public async Task ConnectThenSubscribeUnauthorizedChannelsAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeUnauthoriser());
            var subscribedEvent = new AutoResetEvent(false);
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + "-unauth",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + "-unauth",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
            };

            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == channelNames[2])
                {
                    subscribedEvent.Set();
                }
            };

            int exceptionCount = 0;
            int errorCount = 0;
            pusher.Error += (sender, error) =>
            {
                if (error is ChannelUnauthorizedException)
                {
                    errorCount++;
                }
            };

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            foreach (string channelName in channelNames)
            {
                try
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }
                catch (ChannelUnauthorizedException e)
                {
                    exceptionCount++;
                    Channel channel = e.Channel;
                    Assert.IsNotNull(channel);
                    Assert.IsFalse(channel.IsSubscribed);
                }
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(2, exceptionCount, "Number of exceptions expected");
            Assert.AreEqual(2, errorCount, "Number of errors expected");
            AssertUnauthorized(pusher, channelNames);
        }

        #endregion

        #region Private static methods

        private static async Task ConnectThenSubscribeTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: true, channelType, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private static async Task ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: true, channelType).ConfigureAwait(false);
        }

        private static async Task ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelMultipleTimesTestAsync(connectBeforeSubscribing: true, channelType).ConfigureAwait(false);
        }

        private static async Task ConnectThenSubscribeMultipleChannelsTestAsync(Pusher pusher, IList<string> channelNames)
        {
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
        }

        #endregion
    }
}
