using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;
using WebSocket4Net;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests subscribe and unsubscribe functionality for Public, Private and Presence channels.
    /// </summary>
    [TestFixture]
    public partial class SubscriptionTest
    {
        private readonly List<Pusher> _clients = new List<Pusher>(10);

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
        }

        #region Connect then subscribe combined channel tests

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames();

            // Act and Assert
            await ConnectThenSubscribeMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeThenDisconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            List<string> channelNames = CreateChannelNames();

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
        public async Task CombinedChannelsConnectThenSubscribeThenDisconnectThenReconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames();
            var subscribedEvent = new AutoResetEvent(false);
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
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
        public async Task CombinedChannelsConnectThenSubscribeUnauthorizedAsync()
        {
            await SubscribeUnauthorizedChannelsAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeAuthorizationFailureAsync()
        {
            await SubscribeAuthorizationFailureChannelsAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeMessageTamperFailureAsync()
        {
            await SubscribeFailureChannelsAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeThenUnsubscribeTest()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            foreach (string channelName in channelNames)
            {
                await pusher.UnsubscribeAsync(channelName).ConfigureAwait(false);
            }

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        [Test]
        public async Task CombinedChannelsConnectThenSubscribeThenUnsubscribeAllTest()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            await pusher.UnsubscribeAllAsync().ConfigureAwait(false);

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        #endregion

        #region Subscribe then connect combined channel tests

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames();

            // Act and Assert
            await SubscribeThenConnectMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectThenDisconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames();
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
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
        public async Task CombinedChannelsSubscribeThenConnectThenDisconnectThenReconnectAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames();
            var subscribedEvent = new AutoResetEvent(false);
            var disconnectedEvent = new AutoResetEvent(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
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
        public async Task CombinedChannelsSubscribeThenConnectThenReconnectWhenTheUnderlyingSocketIsClosedAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            var subscribedEvent = new AutoResetEvent(false);
            List<string> channelNames = CreateChannelNames();

            // Act
            await SubscribeThenConnectMultipleChannelsTestAsync(pusher, channelNames).ConfigureAwait(false);
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == channelNames.Count)
                {
                    subscribedEvent.Set();
                }
            };
            await Task.Run(() =>
            {
                WebSocket socket = ConnectionTest.GetWebSocket(pusher);
                socket.Close();
            }).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(subscribedEvent.WaitOne(TimeSpan.FromSeconds(5)));
            AssertIsSubscribed(pusher, channelNames);
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectUnauthorizedAsync()
        {
            await SubscribeUnauthorizedChannelsAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectAuthorizationFailureAsync()
        {
            await SubscribeAuthorizationFailureChannelsAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectMessageTamperFailureAsync()
        {
            await SubscribeFailureChannelsAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectThenUnsubscribeTest()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            foreach (string channelName in channelNames)
            {
                await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
            }

            await pusher.ConnectAsync().ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            foreach (string channelName in channelNames)
            {
                await pusher.UnsubscribeAsync(channelName).ConfigureAwait(false);
            }

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        [Test]
        public async Task CombinedChannelsSubscribeThenConnectThenUnsubscribeAllTest()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            foreach (string channelName in channelNames)
            {
                await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
            }

            await pusher.ConnectAsync().ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            await pusher.UnsubscribeAllAsync().ConfigureAwait(false);

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        #endregion

        #region Subscription backlog tests

        [Test]
        public async Task UnsubscribeWithBacklogTest()
        {
            /*
             *  Test provides code coverage for Pusher.Backlog scenarios.
             */

            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            await pusher.DisconnectAsync().ConfigureAwait(false);
            foreach (string channelName in channelNames)
            {
                await pusher.UnsubscribeAsync(channelName).ConfigureAwait(false);
            }

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        [Test]
        public async Task UnsubscribeAllWithBacklogTest()
        {
            /*
             *  Test provides code coverage for Pusher.Backlog scenarios.
             */

            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser(UserNameFactory.CreateUniqueUserName()), saveTo: _clients);
            List<string> channelNames = CreateChannelNames(numberOfChannels: 6);

            // Act
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
            IList<Channel> channels = pusher.GetAllChannels();
            await pusher.DisconnectAsync().ConfigureAwait(false);
            await pusher.UnsubscribeAllAsync().ConfigureAwait(false);

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Assert
            foreach (Channel channel in channels)
            {
                ValidateUnsubscribedChannel(pusher, channel);
            }
        }

        #endregion

        #region Validation functions

        internal static void ValidateUnsubscribedChannel(Pusher pusher, Channel unsubscribedChannel)
        {
            Assert.IsNotNull(unsubscribedChannel);
            Assert.IsFalse(string.IsNullOrWhiteSpace(unsubscribedChannel.Name));
            Assert.IsNull(pusher.GetChannel(unsubscribedChannel.Name), $"Channel {unsubscribedChannel.Name} should not exist.");
        }

        private static void ValidateSubscribedChannel(Pusher pusher, string expectedChannelName, Channel channel, ChannelTypes expectedChannelType)
        {
            ValidateChannel(pusher, expectedChannelName, channel, expectedChannelType, true);
        }

        private static void ValidateDisconnectedChannel(Pusher pusher, string expectedChannelName, Channel channel, ChannelTypes expectedChannelType)
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
            ValidateChannel(channel, expectedChannelType, gotChannel, isSubscribed);

            // Validate GetAllChannels results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsNotNull(channels);
            Assert.IsTrue(channels.Count >= 1);
            Channel actualChannel = channels.Where((c) => c.Name.Equals(expectedChannelName)).SingleOrDefault();
            ValidateChannel(channel, expectedChannelType, actualChannel, isSubscribed);
        }

        private static void ValidateChannel(Channel expectedChannel, ChannelTypes expectedChannelType, Channel actualChannel, bool isSubscribed)
        {
            Assert.IsNotNull(actualChannel);
            Assert.AreEqual(expectedChannel.Name, actualChannel.Name, nameof(Channel.Name));
            Assert.AreEqual(isSubscribed, actualChannel.IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(expectedChannelType, actualChannel.ChannelType, nameof(Channel.ChannelType));
        }

        private static void ValidateSubscribedExceptions(string mockChannelName, SubscribedEventHandlerException[] errors)
        {
            foreach (var error in errors)
            {
                Assert.IsNotNull(error, "Expected a SubscribedDelegateException error to be raised.");
                Assert.IsNotNull(error.MessageData, nameof(SubscribedEventHandlerException.MessageData));
                Assert.IsNotNull(error.Channel, nameof(SubscribedEventHandlerException.Channel));
                Assert.AreEqual(mockChannelName, error.Channel.Name, nameof(Channel.Name));
            }
        }

        private static void AssertIsSubscribed(Pusher pusher, IList<string> channelNames)
        {
            foreach (string channelName in channelNames)
            {
                Channel channel = pusher.GetChannel(channelName);
                Assert.IsNotNull(channel, $"Channel {channelName} should be found");
                Assert.IsTrue(channel.IsSubscribed, $"Expected {channel.Name} to be subscribed");
            }
        }

        private static void AssertIsDisconnected(Pusher pusher, IList<string> channelNames)
        {
            foreach (string channelName in channelNames)
            {
                Channel channel = pusher.GetChannel(channelName);
                Assert.IsNotNull(channel, $"Channel {channelName} should be found");
                Assert.IsFalse(channel.IsSubscribed, $"Expected {channel.Name} to be disconnected");
            }
        }

        private static void AssertUnauthorized(Pusher pusher, List<string> channelNames)
        {
            foreach (string channelName in channelNames)
            {
                Channel channel = pusher.GetChannel(channelName);
                if (Channel.GetChannelType(channelName) == ChannelTypes.Public)
                {
                    Assert.IsNotNull(channel, $"Channel {channelName} should be found");
                    Assert.IsTrue(channel.IsSubscribed, $"Expected {channel.Name} to be subscribed");
                }
                else
                {
                    Assert.IsNull(channel, $"Channel {channelName} should not be found");
                }
            }
        }

        #endregion

        #region Subscribe test methods

        private static List<string> CreateChannelNames(int numberOfChannels = 3)
        {
            List<string> result = new List<string>(numberOfChannels);
            for (int i = 0; i < numberOfChannels; i++)
            {
                result.Add(ChannelNameFactory.CreateUniqueChannelName(channelType: (ChannelTypes)(i % 3)));
            }

            return result;
        }

        private async Task SubscribeWithoutConnectingTestAsync(ChannelTypes channelType)
        {
            // Arrange
            Pusher pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            string mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);

            // Act
            Channel subscribedChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Assert
            ValidateDisconnectedChannel(pusher, mockChannelName, subscribedChannel, channelType);
        }

        private async Task SubscribeThenUnsubscribeWithoutConnectingTestAsync(ChannelTypes channelType)
        {
            // Arrange
            string mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            Pusher pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);

            // Act
            Channel subscribedChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            await pusher.UnsubscribeAsync(subscribedChannel.Name).ConfigureAwait(false);

            // Assert
            ValidateUnsubscribedChannel(pusher, subscribedChannel);
        }

        private async Task SubscribeTestAsync(bool connectBeforeSubscribing, ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            // Arrange
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            AutoResetEvent[] errorEvent = { null, null };
            string mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            if (pusher == null)
            {
                pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            }

            bool[] channelSubscribed = { false, false };

            void PusherSubscribedEventHandler(object sender, Channel channel)
            {
                if (channel.Name == mockChannelName)
                {
                    channelSubscribed[0] = true;
                    subscribedEvent.Set();
                    if (raiseSubscribedError)
                    {
                        throw new InvalidOperationException($"Simulated error for {nameof(Pusher)}.{nameof(Pusher.Subscribed)} {channel.Name}.");
                    }
                }
            }

            pusher.Subscribed += PusherSubscribedEventHandler;

            SubscribedEventHandlerException[] errors = { null, null };
            if (raiseSubscribedError)
            {
                errorEvent[0] = new AutoResetEvent(false);
                errorEvent[1] = new AutoResetEvent(false);

                void ErrorHandler(object sender, PusherException error)
                {
                    if (error.ToString().Contains($"{nameof(Pusher)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[0] = error as SubscribedEventHandlerException;
                        errorEvent[0].Set();
                    }
                    else if (error.ToString().Contains($"{nameof(Channel)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[1] = error as SubscribedEventHandlerException;
                        errorEvent[1].Set();
                    }
                }

                pusher.Error += ErrorHandler;
            }

            void ChannelSubscribedEventHandler(object sender)
            {
                channelSubscribed[1] = true;
                if (raiseSubscribedError)
                {
                    throw new InvalidOperationException($"Simulated error for {nameof(Channel)}.{nameof(Pusher.Subscribed)} {mockChannelName}.");
                }
            }

            // Act
            Channel subscribedChannel;
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                subscribedChannel = await pusher.SubscribeAsync(mockChannelName, ChannelSubscribedEventHandler).ConfigureAwait(false);
            }
            else
            {
                subscribedChannel = await pusher.SubscribeAsync(mockChannelName, ChannelSubscribedEventHandler).ConfigureAwait(false);
                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[0]?.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[1]?.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, subscribedChannel, channelType);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private async Task SubscribeSameChannelTwiceAsync(bool connectBeforeSubscribing, ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType, saveTo: _clients);
            var subscribedEvent = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType);
            var numberOfCalls = 0;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == mockChannelName)
                {
                    numberOfCalls++;
                    channelSubscribed = true;
                    subscribedEvent.Set();
                }
            };

            Channel firstChannel;
            Channel secondChannel;

            // Act
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
                secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            }
            else
            {
                firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
                secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.AreEqual(firstChannel.IsSubscribed, secondChannel.IsSubscribed);
            Assert.AreEqual(firstChannel.Name, secondChannel.Name);
            Assert.AreEqual(firstChannel.ChannelType, secondChannel.ChannelType);
        }

        private async Task SubscribeSameChannelMultipleTimesTestAsync(bool connectBeforeSubscribing, ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType, saveTo: _clients);
            var subscribedEvent = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType);
            var numberOfCalls = 0;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == mockChannelName)
                {
                    numberOfCalls++;
                    channelSubscribed = true;
                    subscribedEvent.Set();
                }
            };

            // Act
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                for (int i = 0; i < 4; i++)
                {
                    await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
                };
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
                };

                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        private static async Task SubscribeMultipleChannelsTestAsync(bool connectBeforeSubscribing, Pusher pusher, IList<string> channelNames)
        {
            // Arrange
            var subscribedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedCount++;
                if (subscribedCount == channelNames.Count)
                {
                    subscribedEvent.Set();
                }
            };

            // Act
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                foreach (string channelName in channelNames)
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }
            }
            else
            {
                foreach (string channelName in channelNames)
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }
                AssertIsDisconnected(pusher, channelNames);
                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            // Assert
            Assert.IsTrue(subscribedEvent.WaitOne(TimeSpan.FromSeconds(5)));
            AssertIsSubscribed(pusher, channelNames);
        }

        private async Task SubscribeUnauthorizedChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelUnauthoriser(), saveTo: _clients);
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + FakeChannelUnauthoriser.UnauthoriseToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + FakeChannelUnauthoriser.UnauthoriseToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
            };

            int expectedExceptionCount = 0;
            int exceptionCount = 0;
            int expectedErrorCount = 0;
            foreach (string name in channelNames)
            {
                if (Channel.GetChannelType(name) != ChannelTypes.Public)
                {
                    expectedErrorCount++;
                    if (connectBeforeSubscribing)
                    {
                        expectedExceptionCount++;
                    }
                }
            }

            pusher.Error += (sender, error) =>
            {
                if (!error.EmittedToErrorHandler && error is ChannelUnauthorizedException)
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
            if (connectBeforeSubscribing)
            {
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
                        Assert.IsNotNull(e.AuthorizationEndpoint);
                        Assert.IsNotNull(e.ChannelName);
                        Assert.IsNotNull(e.SocketID);
                        Channel channel = e.Channel;
                        Assert.IsNotNull(channel);
                        Assert.IsFalse(channel.IsSubscribed);
                    }
                }
            }
            else
            {
                foreach (string channelName in channelNames)
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }

                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(expectedExceptionCount, exceptionCount, "Number of exceptions expected");
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors expected");
            AssertUnauthorized(pusher, channelNames);
        }

        private async Task SubscribeAuthorizationFailureChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelUnauthoriser(), saveTo: _clients);
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + "-error",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + "-error",
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
            };

            int expectedExceptionCount = 0;
            int exceptionCount = 0;
            int expectedErrorCount = 0;
            foreach (string name in channelNames)
            {
                if (Channel.GetChannelType(name) != ChannelTypes.Public)
                {
                    expectedErrorCount++;
                    if (connectBeforeSubscribing)
                    {
                        expectedExceptionCount++;
                    }
                }
            }

            pusher.Error += (sender, error) =>
            {
                if (!error.EmittedToErrorHandler && error is ChannelAuthorizationFailureException)
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
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                foreach (string channelName in channelNames)
                {
                    try
                    {
                        await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                    }
                    catch (ChannelAuthorizationFailureException e)
                    {
                        exceptionCount++;
                        Assert.IsNotNull(e.AuthorizationEndpoint);
                        Assert.IsNotNull(e.ChannelName);
                        Assert.IsNotNull(e.SocketID);
                        Channel channel = e.Channel;
                        Assert.IsNotNull(channel);
                        Assert.IsFalse(channel.IsSubscribed);
                    }
                }
            }
            else
            {
                foreach (string channelName in channelNames)
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }

                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(expectedExceptionCount, exceptionCount, "Number of exceptions expected");
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors expected");
            AssertUnauthorized(pusher, channelNames);
        }

        private async Task SubscribeFailureChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeChannelAuthoriser("SabotagedUser"), saveTo: _clients);
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + FakeChannelAuthoriser.TamperToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + FakeChannelAuthoriser.TamperToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public),
            };

            int expectedExceptionCount = 0;
            int exceptionCount = 0;
            int expectedErrorCount = 0;
            foreach (string name in channelNames)
            {
                if (Channel.GetChannelType(name) != ChannelTypes.Public)
                {
                    expectedErrorCount++;
                    if (connectBeforeSubscribing)
                    {
                        expectedExceptionCount++;
                    }
                }
            }

            pusher.Error += (sender, error) =>
            {
                if (!error.EmittedToErrorHandler && error is ChannelException)
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
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                foreach (string channelName in channelNames)
                {
                    try
                    {
                        await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                    }
                    catch (ChannelException e)
                    {
                        exceptionCount++;
                        Assert.IsNotNull(e.ChannelName);
                        Assert.IsNotNull(e.SocketID);
                        Channel channel = e.Channel;
                        Assert.IsNotNull(channel);
                        Assert.IsFalse(channel.IsSubscribed);
                    }
                }
            }
            else
            {
                foreach (string channelName in channelNames)
                {
                    await pusher.SubscribeAsync(channelName).ConfigureAwait(false);
                }

                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(expectedExceptionCount, exceptionCount, "Number of exceptions expected");
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors expected");
            AssertUnauthorized(pusher, channelNames);
        }

        #endregion

        #region Connect then subscribe test methods

        private async Task ConnectThenSubscribeTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: true, channelType, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private async Task ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: true, channelType).ConfigureAwait(false);
        }

        private async Task ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelMultipleTimesTestAsync(connectBeforeSubscribing: true, channelType).ConfigureAwait(false);
        }

        private static async Task ConnectThenSubscribeMultipleChannelsTestAsync(Pusher pusher, IList<string> channelNames)
        {
            await SubscribeMultipleChannelsTestAsync(connectBeforeSubscribing: true, pusher, channelNames).ConfigureAwait(false);
        }

        #endregion

        #region Subscribe then connect test methods

        private async Task SubscribeThenConnectTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: false, channelType, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private async Task SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes channelType)
        {
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: false, channelType).ConfigureAwait(false);
        }

        private async Task SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
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
