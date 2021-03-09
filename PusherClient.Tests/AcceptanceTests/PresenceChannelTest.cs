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
    public class PresenceChannelTest
    {
        [Test]
        public async Task ConnectThenSubscribeChannelAsync()
        {
            await ConnectThenSubscribeTestAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSameChannelTwiceAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
            var firstChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
            var secondChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        [Test]
        public async Task ConnectThenSubscribeSameChannelMultipleTimesAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
                    return pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName);
                }));
            };

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            PusherException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeTestAsync(pusher: pusher).ConfigureAwait(false);
            }
            catch (PusherException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An Authorizer needs to be provided when subscribing to a private or presence channel.", caughtException.Message);
        }

        [Test]
        public async Task SubscribeThenConnectChannelAsync()
        {
            await SubscribeThenConnectTestAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSameChannelTwiceAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
            var firstChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
            var secondChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Delay for enough time to be sure numberOfCalls is not greater than one
            await Task.Delay(1000).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        [Test]
        public async Task SubscribeThenConnectSameChannelMultipleTimesAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
                await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
            };

            await pusher.ConnectAsync().ConfigureAwait(false);

            // Delay for enough time to be sure numberOfCalls is not greater than one
            await Task.Delay(1500).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        private static async Task ConnectThenSubscribeTestAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
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
            var channel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, subscribedEventHandler).ConfigureAwait(false);

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private static async Task SubscribeThenConnectTestAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
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
            var channel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
            await pusher.ConnectAsync().ConfigureAwait(false);
            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[0]?.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[1]?.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private static void ValidateSubscribedChannel(Pusher pusher, string expectedChannelName, Channel channel)
        {
            ValidateChannel(pusher, expectedChannelName, channel, true);
        }

        private static void ValidateDisconnectedChannel(Pusher pusher, string expectedChannelName, Channel channel)
        {
            ValidateChannel(pusher, expectedChannelName, channel, false);
        }

        private static void ValidateChannel(Pusher pusher, string expectedChannelName, Channel channel, bool isSubscribed)
        {
            Assert.IsNotNull(channel);
            StringAssert.Contains(expectedChannelName, channel.Name);
            Assert.AreEqual(isSubscribed, channel.IsSubscribed, nameof(Channel.IsSubscribed));

            // Validate GetChannel result
            Channel gotChannel = pusher.GetChannel(expectedChannelName);
            ValidateChannel(channel, gotChannel, isSubscribed);

            // Validate GetAllChannels results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsNotNull(channels);
            Assert.IsTrue(channels.Count >= 1);
            Channel actualChannel = channels.Where((c) => c.Name.Equals(expectedChannelName)).SingleOrDefault();
            ValidateChannel(channel, actualChannel, isSubscribed);
        }

        private static void ValidateChannel(Channel expectedChannel, Channel actualChannel, bool isSubscribed)
        {
            Assert.IsNotNull(actualChannel);
            Assert.AreEqual(expectedChannel.Name, actualChannel.Name, nameof(Channel.Name));
            Assert.AreEqual(isSubscribed, actualChannel.IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(ChannelTypes.Presence, actualChannel.ChannelType, nameof(Channel.ChannelType));
        }

        private static void ValidateSubscribedExceptions(string mockChannelName, SubscribedDelegateException[] errors)
        {
            foreach (var error in errors)
            {
                Assert.IsNotNull(error, "Expected a SubscribedDelegateException error to be raised.");
                Assert.IsNotNull(error.MessageData, nameof(SubscribedDelegateException.MessageData));
                Assert.AreEqual(mockChannelName, error.ChannelName, nameof(SubscribedDelegateException.ChannelName));
            }
        }
    }
}
