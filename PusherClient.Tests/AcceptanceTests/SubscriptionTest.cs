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
    public partial class SubscriptionTest
    {
         [Test]
        public async Task PusherShouldUnsubscribeSuccessfullyWhenTheRequestIsMadeViaTheChannelAsync()
        {
            /*
             *  This test is for exisitng functionality that is wrong. 
             *  Unsubscribe should really remove the subscription and not just mark IsSubscribed to false.
             *  If you disconnect and then reconnect the channel will be subscribed again.
             */

            // Arrange
            var pusher = PusherFactory.GetPusher();
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            channel.Unsubscribe();

            // Assert
            ValidateDisconnectedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
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

        private static async Task SubscribeTestAsync(bool connectBeforeSubscribing, ChannelTypes channelType, Pusher pusher = null, bool raiseSubscribedError = false)
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
            pusher.Subscribed += (sender, channel) =>
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
            };

            SubscribedEventHandlerException[] errors = { null, null };
            if (raiseSubscribedError)
            {
                errorEvent[0] = new AutoResetEvent(false);
                errorEvent[1] = new AutoResetEvent(false);
                pusher.Error += (sender, error) =>
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
            Channel subscribedChannel;
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                subscribedChannel = await pusher.SubscribeAsync(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
            }
            else
            {
                subscribedChannel = await pusher.SubscribeAsync(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
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

        private static async Task SubscribeSameChannelTwiceAsync(bool connectBeforeSubscribing, ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType);
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

        private static async Task SubscribeSameChannelMultipleTimesTestAsync(bool connectBeforeSubscribing, ChannelTypes channelType)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(channelType);
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

        private static async Task SubscribeUnauthorizedChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeUnauthoriser());
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + FakeUnauthoriser.UnauthoriseToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + FakeUnauthoriser.UnauthoriseToken,
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

        private static async Task SubscribeAuthorizationFailureChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeUnauthoriser());
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
                if (error is ChannelAuthorizationFailureException)
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

        private static async Task SubscribeFailureChannelsAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser("SabotagedUser"));
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var errorEvent = new AutoResetEvent(false);
            int errorCount = 0;
            List<string> channelNames = new List<string>
            {
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Private) + FakeAuthoriser.SabotageToken,
                ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence) + FakeAuthoriser.SabotageToken,
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
                if (error is ChannelException)
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
    }
}
