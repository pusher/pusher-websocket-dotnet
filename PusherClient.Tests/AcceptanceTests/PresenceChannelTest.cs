using System;
using System.Collections.Concurrent;
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
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeSameChannelMultipleTimesAsync()
        {
            await SubscribeSameChannelMultipleTimesAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelMultipleMembersAsync()
        {
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            await ConnectThenSubscribeMultipleMembersAsync(4, channelName).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelRemoveMemberAsync()
        {
            // Arrange
            int numberOfMembers = 4;
            int numMembersRemovedEvents = 0;
            int expectedNumMembersRemovedEvents = numberOfMembers - 1;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            AutoResetEvent memberRemovedEvent = new AutoResetEvent(false);

            // Act
            IList<Pusher> pusherMembers = await ConnectThenSubscribeMultipleMembersAsync(numberOfMembers, channelName).ConfigureAwait(false);
            for (int i = 0; i < pusherMembers.Count; i++)
            {
                var presenceChannel = await pusherMembers[i].SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
                presenceChannel.MemberRemoved += (sender, member) =>
                {
                    numMembersRemovedEvents++;
                    if (numMembersRemovedEvents == expectedNumMembersRemovedEvents)
                    {
                        memberRemovedEvent.Set();
                    }
                };
            }

            await pusherMembers[0].DisconnectAsync().ConfigureAwait(false);

            // Assert
            Assert.IsTrue(memberRemovedEvent.WaitOne(TimeSpan.FromSeconds(5)));
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
            await SubscribeSameChannelTwiceAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectSameChannelMultipleTimesAsync()
        {
            await SubscribeSameChannelMultipleTimesAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelMultipleMembersAsync()
        {
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            await SubscribeThenConnectMultipleMembersAsync(4, channelName).ConfigureAwait(false);
        }

        private static async Task SubscribeTestAsync(bool connectBeforeSubscribing, Pusher pusher = null, bool raiseSubscribedError = false)
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

            Channel presenceChannel;

            // Act
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                presenceChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
            }
            else
            {
                presenceChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, subscribedEventHandler).ConfigureAwait(false);
                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[0]?.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[1]?.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, presenceChannel);
            Assert.IsTrue(channelSubscribed[0]);
            Assert.IsTrue(channelSubscribed[1]);
            if (raiseSubscribedError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }
        }

        private static async Task ConnectThenSubscribeTestAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: true, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private static async Task SubscribeThenConnectTestAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeTestAsync(connectBeforeSubscribing: false, pusher, raiseSubscribedError).ConfigureAwait(false);
        }

        private static async Task SubscribeSameChannelTwiceAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
                firstChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
                secondChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
            }
            else
            {
                firstChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
                secondChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
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

        private static async Task SubscribeSameChannelMultipleTimesAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence);
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(ChannelTypes.Presence);
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
                    await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
                };
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);
                };

                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
        }

        private static async Task<IList<Pusher>> SubscribeMultipleMembersAsync(bool connectBeforeSubscribing, int numberOfMembers, string channelName)
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            List<Pusher> pusherMembers = new List<Pusher>(numberOfMembers);
            ConcurrentDictionary<string, AutoResetEvent> memberAddedEvents = new ConcurrentDictionary<string, AutoResetEvent>();
            int subscribedCount = 0;
            int expectedSubscribedCount = numberOfMembers;
            for (int i = 1; i <= numberOfMembers; i++)
            {
                Pusher pusher = PusherFactory.GetPusher(channelType: channelType, $"User{i}");
                pusherMembers.Add(pusher);
                pusher.Connected += (sender) =>
                {
                    memberAddedEvents[((Pusher)sender).SocketID] = new AutoResetEvent(false);
                };

                pusher.Subscribed += (sender, channel) =>
                {
                    subscribedCount++;
                    if (subscribedCount == expectedSubscribedCount)
                    {
                        subscribedEvent.Set();
                    }
                };
            }

            List<GenericPresenceChannel<FakeUserInfo>> presenceChannels = new List<GenericPresenceChannel<FakeUserInfo>>(pusherMembers.Count);
            AutoResetEvent memberAddedEvent = new AutoResetEvent(false);
            int addedCount = 0;
            int expectedAddedCount = Factorial(pusherMembers.Count - 1);

            // Act
            if (connectBeforeSubscribing)
            {
                foreach (var pusher in pusherMembers)
                {
                    await pusher.ConnectAsync().ConfigureAwait(false);
                }
            }

            for (int i = 0; i < pusherMembers.Count; i++)
            {
                var presenceChannel = await pusherMembers[i].SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
                presenceChannel.MemberAdded += (object sender, KeyValuePair<string, FakeUserInfo> member) =>
                {
                    addedCount++;
                    if (addedCount == expectedAddedCount)
                    {
                        memberAddedEvent.Set();
                    }
                };
                presenceChannels.Add(presenceChannel);
            }

            if (!connectBeforeSubscribing)
            {
                foreach (var pusher in pusherMembers)
                {
                    await pusher.ConnectAsync().ConfigureAwait(false);
                }
            }

            // Assert
            Assert.IsTrue(subscribedEvent.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.IsTrue(memberAddedEvent.WaitOne(TimeSpan.FromSeconds(10)));
            for (int i = 0; i < pusherMembers.Count; i++)
            {
                ValidateSubscribedChannel(pusherMembers[i], channelName, presenceChannels[i], numMembersExpected: pusherMembers.Count);
            }

            return pusherMembers;
        }

        private static async Task<IList<Pusher>> ConnectThenSubscribeMultipleMembersAsync(int numberOfMembers, string channelName)
        {
            return await SubscribeMultipleMembersAsync(connectBeforeSubscribing: true, numberOfMembers, channelName).ConfigureAwait(false);
        }

        private static async Task<IList<Pusher>> SubscribeThenConnectMultipleMembersAsync(int numberOfMembers, string channelName)
        {
            return await SubscribeMultipleMembersAsync(connectBeforeSubscribing: false, numberOfMembers, channelName).ConfigureAwait(false);
        }

        private static void ValidateSubscribedChannel(Pusher pusher, string expectedChannelName, Channel channel, int numMembersExpected = 1)
        {
            ValidateChannel(pusher, expectedChannelName, channel, true, numMembersExpected);
        }

        private static void ValidateDisconnectedChannel(Pusher pusher, string expectedChannelName, Channel channel, int numMembersExpected = 1)
        {
            ValidateChannel(pusher, expectedChannelName, channel, false, numMembersExpected);
        }

        private static void ValidateChannel(Pusher pusher, string expectedChannelName, Channel channel, bool isSubscribed, int numMembersExpected)
        {
            Assert.IsNotNull(channel);
            StringAssert.Contains(expectedChannelName, channel.Name);
            Assert.AreEqual(isSubscribed, channel.IsSubscribed, nameof(Channel.IsSubscribed));

            // Validate GetChannel result
            Channel gotChannel = pusher.GetChannel(expectedChannelName);
            ValidateChannel(channel, gotChannel, isSubscribed, numMembersExpected);

            // Validate GetAllChannels results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsNotNull(channels);
            Assert.IsTrue(channels.Count >= 1);
            Channel actualChannel = channels.Where((c) => c.Name.Equals(expectedChannelName)).SingleOrDefault();
            ValidateChannel(channel, actualChannel, isSubscribed, numMembersExpected);
        }

        private static void ValidateChannel(Channel expectedChannel, Channel actualChannel, bool isSubscribed, int numMembersExpected)
        {
            Assert.IsNotNull(actualChannel);
            Assert.AreEqual(expectedChannel.Name, actualChannel.Name, nameof(Channel.Name));
            Assert.AreEqual(isSubscribed, actualChannel.IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(ChannelTypes.Presence, actualChannel.ChannelType, nameof(Channel.ChannelType));

            IPresenceChannel<FakeUserInfo> presenceChannel = actualChannel as IPresenceChannel<FakeUserInfo>;
            Assert.IsNotNull(presenceChannel);

            Dictionary<string, FakeUserInfo> members = presenceChannel.GetMembers();
            Assert.IsNotNull(members);
            Assert.AreEqual(numMembersExpected, members.Count, "# Members");

            foreach (var member in members)
            {
                FakeUserInfo actualMember = presenceChannel.GetMember(member.Key);
                Assert.AreEqual(member.Value.name, actualMember.name);
            }
        }

        private static void ValidateSubscribedExceptions(string mockChannelName, SubscribedDelegateException[] errors)
        {
            foreach (var error in errors)
            {
                Assert.IsNotNull(error, "Expected a SubscribedDelegateException error to be raised.");
                Assert.IsNotNull(error.MessageData, nameof(SubscribedDelegateException.MessageData));
                Assert.IsNotNull(error.Channel, nameof(SubscribedDelegateException.Channel));
                Assert.AreEqual(mockChannelName, error.Channel.Name, nameof(Channel.Name));
            }
        }

        private static int Factorial(int i)
        {
            if (i <= 1) return 1;
            return i * Factorial(i - 1);
        }
    }
}
