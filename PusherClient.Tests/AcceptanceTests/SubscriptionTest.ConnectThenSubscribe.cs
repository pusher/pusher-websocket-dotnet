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

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Public);
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private);
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName1, channel1, ChannelTypes.Public);
            ValidateSubscribedChannel(pusher, mockChannelName2, channel2, ChannelTypes.Private);
            ValidateSubscribedChannel(pusher, mockChannelName3, channel3, ChannelTypes.Presence);
        }

        [Test]
        public async Task ConnectThenSubscribeAllChannelsThenDisconnectAsync()
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
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
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
        public async Task ConnectThenSubscribeAllChannelsThenDisconnectThenReconnectAsync()
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
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel1 = await pusher.SubscribeAsync(mockChannelName1).ConfigureAwait(false);
            var channel2 = await pusher.SubscribeAsync(mockChannelName2).ConfigureAwait(false);
            var channel3 = await pusher.SubscribeAsync(mockChannelName3).ConfigureAwait(false);
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

        private static async Task ConnectThenSubscribeTestAsync(ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
        {
            // Arrange
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
                    if (raiseSubscribedError)
                    {
                        throw new InvalidOperationException($"Simulated error for {nameof(Pusher)}.{nameof(Pusher.Subscribed)} {channelName}.");
                    }
                }
            };

            SubscribedDelegateException[] errors = { null, null };
            if (raiseSubscribedError)
            {
                pusher.Error += (sender, error) =>
                {
                    if (error.ToString().Contains($"{nameof(Pusher)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[0] = error as SubscribedDelegateException;
                    }
                    else if (error.ToString().Contains($"{nameof(Channel)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[1] = error as SubscribedDelegateException;
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
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel = await pusher.SubscribeAsync(mockChannelName, subscribedEventHandler).ConfigureAwait(false);

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, channelType);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private static async Task ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes channelType)
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
            await pusher.ConnectAsync().ConfigureAwait(false);
            var firstChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            var secondChannel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        private static async Task ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes channelType)
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
            await pusher.ConnectAsync().ConfigureAwait(false);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.SubscribeAsync(mockChannelName);
                }));
            };

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        #endregion
    }
}
