using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Subscription
    {
        [Test]
        public async Task PusherShouldSubscribeToAChannelWhenGivenAPopulatedPublicChannelNameAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    channelSubscribed = true;
                    reset.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public async Task PusherShouldSubscribeToAChannelWhenGivenAPopulatedPublicChannelNameEvenWhenSubscribedErrorsAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            bool errored = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                throw new InvalidOperationException($"Simulated error for Subscribed {channelName}.");
            };
            pusher.Error += (sender, error) =>
            {
                errored = true;
                reset.Set();
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
            Assert.IsTrue(errored);
        }

        [Test]
        public async Task PusherShouldSubscribeToAChannelWhenGivenAPopulatedPrivateChannelNameAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(privateChannel: true);
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    channelSubscribed = true;
                    reset.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Private);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public async Task PusherShouldSubscribeToAChannelWhenGivenAPopulatedPresenceChannelNameAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(presenceChannel: true);
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    channelSubscribed = true;
                    reset.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(10));

            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Presence);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public async Task PusherShouldNotCreateAnotherSubscriptionToAChannelIfTheChannelHasAlreadyBeenSubscribedToAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            var numberOfCalls = 0;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    numberOfCalls++;
                    channelSubscribed = true;
                    reset.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            var firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            var secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        [Test]
        public async Task PusherShouldNotAttemptASecondChannelSubscriptionToAnExistingChannelWhileTheFirstRequestIsWaitingForAResponseAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            var connectedEvent = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            var subscribedEvent = new AutoResetEvent(false);
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    subscribedEvent.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            var firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Act
            var secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
        }

        [Test]
        public async Task PusherShouldUnsubscribeSuccessfullyWhenTheRequestIsMadeViaTheChannelAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    reset.Set();
                }
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            channel.Unsubscribe();

            // Assert
            ValidateUnsubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
        }

        [Test]
        public async Task PusherShouldUnsubscribeSuccessfullyWhenTheRequestIsMadeViaThePusherObjectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();
            var subscribedEvent = new AutoResetEvent(false);
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    subscribedEvent.Set();
                }
            };

            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            await pusher.ConnectAsync().ConfigureAwait(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            channel.Unsubscribe();

            // Assert
            ValidateUnsubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
        }

        [Test]
        public async Task PusherShouldUnsubscribeAllTheSubscribedChannelsWhenTheConnectionIsDisconnectedAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
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

            await pusher.ConnectAsync().ConfigureAwait(false);

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            try
            {
                await pusher.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // Assert
            ValidateUnsubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateUnsubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Public);
            ValidateUnsubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Public);
        }

        [Test]
        public async Task PusherShouldSubscribeAllExistingChannelsWhenTheConnectionIsConnectedAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
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

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Public);
        }

        [Test]
        public async Task PusherShouldSubscribeAllPreviouslySubscribedChannelsWhenTheConnectionIsReconnectedAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
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

            await pusher.ConnectAsync().ConfigureAwait(false);

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            await pusher.DisconnectAsync().ConfigureAwait(false);

            subscribedEvent.Reset();
            subscribedCount = 0;

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Public);
        }

        private static void ValidateSubscribedChannel(Pusher pusher, string expectedChannelName, Channel channel, ChannelTypes expectedChannelType)
        {
            ValidateChannel(pusher, expectedChannelName, channel, expectedChannelType, true);
        }

        private static void ValidateUnsubscribedChannel(Pusher pusher, string expectedChannelName, Channel channel, ChannelTypes expectedChannelType)
        {
            ValidateChannel(pusher, expectedChannelName, channel, expectedChannelType, false);
        }

        private static void ValidateChannel(Pusher pusher, string expectedChannelName, Channel channel, ChannelTypes expectedChannelType, bool isSubscribed)
        {
            Assert.IsNotNull(channel);
            StringAssert.Contains(expectedChannelName, channel.Name);
            Assert.AreEqual(isSubscribed, channel.IsSubscribed, nameof(Channel.IsSubscribed));

            // Validate GetChannel result
            Channel gotChannel = pusher.GetChannel(expectedChannelName);
            ValidateChannelInfo(channel, expectedChannelType, isSubscribed, gotChannel);

            // Validate GetAllChannels results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsNotNull(channels);
            Assert.IsTrue(channels.Count >= 1);
            Channel channelInfo = channels.Where((c) => c.Name.Equals(expectedChannelName)).SingleOrDefault();
            ValidateChannelInfo(channel, expectedChannelType, isSubscribed, channelInfo);
        }

        private static void ValidateChannelInfo(Channel channel, ChannelTypes expectedChannelType, bool isSubscribed, Channel channelInfo)
        {
            Assert.IsNotNull(channelInfo);
            Assert.AreEqual(channel.Name, channelInfo.Name, nameof(Channel.Name));
            Assert.AreEqual(isSubscribed, channelInfo.IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(expectedChannelType, channelInfo.ChannelType, nameof(Channel.ChannelType));
        }
    }
}
