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
        private readonly List<Pusher> _clients = new List<Pusher>(10);

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
        }

        #region Connect first then subscribe tests

        [Test]
        public async Task ConnectThenSubscribeChannelAsync()
        {
            await ConnectThenSubscribeAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeAsync(raiseSubscribedError: true).ConfigureAwait(false);
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
        public async Task ConnectThenSubscribeChannelMultipleMembersWithMemberAddedErrorAsync()
        {
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            await ConnectThenSubscribeMultipleMembersAsync(3, channelName, raiseMemberAddedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelRemoveMemberAsync()
        {
            await RemoveMemberAsync(connectBeforeSubscribing: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelRemoveMemberWithMemberRemovedErrorAsync()
        {
            await RemoveMemberAsync(connectBeforeSubscribing: true, numberOfMembers: 3, raiseMemberRemovedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribeChannelWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            ChannelException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeAsync(pusher: pusher).ConfigureAwait(false);
            }
            catch (ChannelException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An Authorizer needs to be provided when subscribing to the private or presence channel", caughtException.Message);
        }

        #endregion

        #region Subscribe first then connect

        [Test]
        public async Task SubscribeThenConnectChannelAsync()
        {
            await SubscribeThenConnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectAsync(raiseSubscribedError: true).ConfigureAwait(false);
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

        [Test]
        public async Task SubscribeThenConnectChannelMultipleMembersWithMemberAddedErrorAsync()
        {
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            await SubscribeThenConnectMultipleMembersAsync(3, channelName, raiseMemberAddedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelRemoveMemberAsync()
        {
            await RemoveMemberAsync(connectBeforeSubscribing: false).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelRemoveMemberWithMemberRemovedErrorAsync()
        {
            await RemoveMemberAsync(connectBeforeSubscribing: false, numberOfMembers: 3, raiseMemberRemovedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectChannelWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            ChannelException caughtException = null;

            // Act
            try
            {
                await SubscribeThenConnectAsync(pusher: pusher).ConfigureAwait(false);
            }
            catch (ChannelException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An Authorizer needs to be provided when subscribing to the private or presence channel", caughtException.Message);
        }

        #endregion

        #region Idempotency, unsubscribe and timeout tests

        [Test]
        public async Task ConcurrentSubscribesShouldBeIdempotentAsync()
        {
            // Arrange
            Channel presenceChannel = null;
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            ChannelTypes channelType = ChannelTypes.Presence;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            Pusher pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            await pusher.ConnectAsync().ConfigureAwait(false);
            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == channelName)
                {
                    subscribedCount++;
                    presenceChannel = channel;
                    subscribedEvent.Set();
                }
            };

            // Act
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.IsTrue(subscribedEvent.WaitOne(TimeSpan.FromSeconds(5)), nameof(subscribedEvent));
            Assert.AreEqual(1, subscribedCount, nameof(subscribedCount));
            ValidateSubscribedChannel(pusher, channelName, presenceChannel);
        }

        [Test]
        public async Task ConcurrentUnsubscribesShouldBeIdempotentAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            Pusher pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            Channel channel = await SubscribeAsync(connectBeforeSubscribing: true, pusher: pusher, channelName: channelName).ConfigureAwait(false);

            // Act
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.UnsubscribeAsync(channelName);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            SubscriptionTest.ValidateUnsubscribedChannel(pusher, channel);
        }

        [Test]
        public async Task UnsubscribeAsync()
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            Pusher pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            Channel channel = await SubscribeAsync(connectBeforeSubscribing: true, pusher: pusher, channelName: channelName).ConfigureAwait(false);

            // Act
            await pusher.UnsubscribeAsync(channelName).ConfigureAwait(false);

            // Assert
            SubscriptionTest.ValidateUnsubscribedChannel(pusher, channel);
        }

        [Test]
        public async Task PusherShouldErrorWhenSubscribeTimesOutAsync()
        {
            // Arrange
            AutoResetEvent errorEvent = new AutoResetEvent(false);
            PusherException exception = null;
            PusherException caughtException = null;

            ChannelTypes channelType = ChannelTypes.Presence;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            var pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            await pusher.ConnectAsync().ConfigureAwait(false);
            ((IPusher)pusher).PusherOptions.ClientTimeout = TimeSpan.FromMilliseconds(20);

            pusher.Error += (sender, error) =>
            {
                exception = error;
                errorEvent.Set();
            };

            // Act
            try
            {
                await pusher.SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                caughtException = error as PusherException;
            }

            // Assert
            Assert.IsNotNull(caughtException, $"Caught exception expected to be {nameof(PusherException)}");
            Assert.IsTrue(errorEvent.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsNotNull(exception, $"Error expected to be {nameof(PusherException)}");
            Assert.AreEqual(exception.Message, caughtException.Message);
            Assert.AreEqual(ErrorCodes.ClientTimeout, exception.PusherCode, "Unexpected error: " + exception.Message);
        }

        #endregion

        #region Private helpers

        private async Task<Channel> SubscribeAsync(bool connectBeforeSubscribing, Pusher pusher = null, string channelName = null, bool raiseError = false)
        {
            // Arrange
            const int PusherSubcribedIndex = 0;
            const int ChannelSubcribedIndex = 1;
            ChannelTypes channelType = ChannelTypes.Presence;
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            AutoResetEvent[] errorEvent = { null, null };
            string mockChannelName = channelName;
            if (mockChannelName == null)
            {
                mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: channelType);
            }

            if (pusher == null)
            {
                pusher = PusherFactory.GetPusher(channelType: channelType, saveTo: _clients);
            }

            bool[] subscribed = { false, false };
            pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == mockChannelName)
                {
                    subscribed[PusherSubcribedIndex] = true;
                    subscribedEvent.Set();
                    if (raiseError)
                    {
                        throw new InvalidOperationException($"Simulated error for {nameof(Pusher)}.{nameof(Pusher.Subscribed)} {channel.Name}.");
                    }
                }
            };

            SubscribedEventHandlerException[] errors = { null, null };
            if (raiseError)
            {
                errorEvent[PusherSubcribedIndex] = new AutoResetEvent(false);
                errorEvent[ChannelSubcribedIndex] = new AutoResetEvent(false);
                pusher.Error += (sender, error) =>
                {
                    if (error.ToString().Contains($"{nameof(Pusher)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[PusherSubcribedIndex] = error as SubscribedEventHandlerException;
                        errorEvent[PusherSubcribedIndex].Set();
                    }
                    else if (error.ToString().Contains($"{nameof(Channel)}.{nameof(Pusher.Subscribed)}"))
                    {
                        errors[ChannelSubcribedIndex] = error as SubscribedEventHandlerException;
                        errorEvent[ChannelSubcribedIndex].Set();
                    }
                };
            }

            void SubscribedEventHandler(object sender)
            {
                subscribed[ChannelSubcribedIndex] = true;
                if (raiseError)
                {
                    throw new InvalidOperationException($"Simulated error for {nameof(Channel)}.{nameof(Pusher.Subscribed)} {mockChannelName}.");
                }
            }


            GenericPresenceChannel<FakeUserInfo> presenceChannel;

            // Act
            if (connectBeforeSubscribing)
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
                presenceChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, SubscribedEventHandler).ConfigureAwait(false);
            }
            else
            {
                presenceChannel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName, SubscribedEventHandler).ConfigureAwait(false);
                await pusher.ConnectAsync().ConfigureAwait(false);
            }

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[PusherSubcribedIndex]?.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent[ChannelSubcribedIndex]?.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, presenceChannel);
            Assert.IsTrue(subscribed[PusherSubcribedIndex]);
            Assert.IsTrue(subscribed[ChannelSubcribedIndex]);
            if (raiseError)
            {
                ValidateSubscribedExceptions(mockChannelName, errors);
            }

            return presenceChannel;
        }

        private async Task ConnectThenSubscribeAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeAsync(connectBeforeSubscribing: true, pusher: pusher, raiseError: raiseSubscribedError).ConfigureAwait(false);
        }

        private async Task SubscribeThenConnectAsync(Pusher pusher = null, bool raiseSubscribedError = false)
        {
            await SubscribeAsync(connectBeforeSubscribing: false, pusher: pusher, raiseError: raiseSubscribedError).ConfigureAwait(false);
        }

        private async Task SubscribeSameChannelTwiceAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence, saveTo: _clients);
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

        private async Task SubscribeSameChannelMultipleTimesAsync(bool connectBeforeSubscribing)
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(ChannelTypes.Presence, saveTo: _clients);
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

        private static void CheckIfAllMembersAdded(int numberOfMembers, ConcurrentDictionary<int, int> membersAddedCounter, AutoResetEvent memberAddedEvent, GenericPresenceChannel<FakeUserInfo> presenceChannel)
        {
            int channelId = presenceChannel.GetHashCode();
            Dictionary<string, FakeUserInfo> members = presenceChannel.GetMembers();
            int savedCount = 0;
            if (!membersAddedCounter.TryAdd(channelId, members.Count))
            {
                savedCount = membersAddedCounter[channelId];
            }

            if (members.Count > savedCount)
            {
                membersAddedCounter[channelId] = members.Count;
                if (members.Count == numberOfMembers)
                {
                    bool done = true;
                    foreach (int memberCount in membersAddedCounter.Values)
                    {
                        if (memberCount != numberOfMembers)
                        {
                            done = false;
                            break;
                        }
                    }

                    if (done)
                    {
                        memberAddedEvent.Set();
                    }
                }
            }
        }

        private async Task<IList<Pusher>> SubscribeMultipleMembersAsync(bool connectBeforeSubscribing, int numberOfMembers, string channelName, bool raiseMemberAddedError = false)
        {
            // Arrange
            ChannelTypes channelType = ChannelTypes.Presence;
            AutoResetEvent subscribedEvent = new AutoResetEvent(false);
            List<Pusher> pusherMembers = new List<Pusher>(numberOfMembers);
            ConcurrentDictionary<int, int> membersAddedCounter = new ConcurrentDictionary<int, int>();
            AutoResetEvent memberAddedEvent = new AutoResetEvent(false);
            int subscribedCount = 0;
            int expectedSubscribedCount = numberOfMembers;
            for (int i = 1; i <= numberOfMembers; i++)
            {
                Pusher pusher = PusherFactory.GetPusher(channelType: channelType, $"User{i}", saveTo: _clients);
                pusherMembers.Add(pusher);
                pusher.Subscribed += (sender, channel) =>
                {
                    CheckIfAllMembersAdded(numberOfMembers, membersAddedCounter, memberAddedEvent, channel as GenericPresenceChannel<FakeUserInfo>);
                    subscribedCount++;
                    if (subscribedCount == expectedSubscribedCount)
                    {
                        subscribedEvent.Set();
                    }
                };
            }

            List<GenericPresenceChannel<FakeUserInfo>> presenceChannels = new List<GenericPresenceChannel<FakeUserInfo>>(pusherMembers.Count);

            // Act
            if (connectBeforeSubscribing)
            {
                foreach (var pusher in pusherMembers)
                {
                    await pusher.ConnectAsync().ConfigureAwait(false);
                }
            }

            AutoResetEvent memberAddedErrorEvent = raiseMemberAddedError ? new AutoResetEvent(false) : null;
            for (int i = 0; i < pusherMembers.Count; i++)
            {
                pusherMembers[i].Error += (sender, error) =>
                {
                    System.Diagnostics.Trace.TraceError($"Pusher.Error handled:{Environment.NewLine}{error}");
                    ValidateMemberAddedEventHandlerException(error, memberAddedErrorEvent);
                };

                var presenceChannel = await pusherMembers[i].SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
                presenceChannel.MemberAdded += (object sender, KeyValuePair<string, FakeUserInfo> member) =>
                {
                    string memberName = "Unknown";
                    bool memberValid = ValidateMember(member);
                    if (memberValid)
                    {
                        memberName = member.Value.name;
                    }

                    if (memberValid)
                    {
                        CheckIfAllMembersAdded(numberOfMembers, membersAddedCounter, memberAddedEvent, sender as GenericPresenceChannel<FakeUserInfo>);
                    }

                    if (raiseMemberAddedError)
                    {
                        throw new InvalidOperationException($"Simulated error for member '{memberName}' when calling GenericPresenceChannel.MemberAdded.");
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
            Assert.IsTrue(subscribedEvent.WaitOne(TimeSpan.FromMilliseconds(FakeAuthoriser.MaxLatency * numberOfMembers)));
            Assert.IsTrue(memberAddedEvent.WaitOne(TimeSpan.FromMilliseconds(FakeAuthoriser.MaxLatency * numberOfMembers)));
            for (int i = 0; i < pusherMembers.Count; i++)
            {
                ValidateSubscribedChannel(pusherMembers[i], channelName, presenceChannels[i], numMembersExpected: pusherMembers.Count);
            }

            if (raiseMemberAddedError)
            {
                Assert.IsTrue(memberAddedErrorEvent.WaitOne(TimeSpan.FromSeconds(5)));
            }

            return pusherMembers;
        }

        private async Task<IList<Pusher>> ConnectThenSubscribeMultipleMembersAsync(int numberOfMembers, string channelName, bool raiseMemberAddedError = false)
        {
            return await SubscribeMultipleMembersAsync(connectBeforeSubscribing: true, numberOfMembers, channelName, raiseMemberAddedError).ConfigureAwait(false);
        }

        private async Task<IList<Pusher>> SubscribeThenConnectMultipleMembersAsync(int numberOfMembers, string channelName, bool raiseMemberAddedError = false)
        {
            return await SubscribeMultipleMembersAsync(connectBeforeSubscribing: false, numberOfMembers, channelName, raiseMemberAddedError).ConfigureAwait(false);
        }

        private async Task RemoveMemberAsync(bool connectBeforeSubscribing, int numberOfMembers = 4, bool raiseMemberRemovedError = false)
        {
            // Arrange
            int numMemberRemovedEvents = 0;
            int expectedNumMemberRemovedEvents = numberOfMembers - 1;
            int expectedNumMemberRemovedErrorEvents = raiseMemberRemovedError ? expectedNumMemberRemovedEvents : 0;
            int numMemberRemovedErrors = 0;
            string channelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            AutoResetEvent memberRemovedEvent = new AutoResetEvent(false);
            AutoResetEvent memberRemovedErrorEvent = raiseMemberRemovedError ? new AutoResetEvent(false) : null;

            void RemoveMemberErrorHandler(object sender, PusherException error)
            {
                System.Diagnostics.Trace.TraceError($"Pusher.Error handled:{Environment.NewLine}{error}");
                numMemberRemovedErrors++;
                ValidateMemberRemovedEventHandlerException(error, expectedNumMemberRemovedErrorEvents, numMemberRemovedErrors, memberRemovedErrorEvent);
            }

            void MemberRemovedEventHandler(object sender, KeyValuePair<string, FakeUserInfo> member)
            {
                string memberName = "Unknown";
                bool memberValid = ValidateMember(member);
                if (memberValid)
                {
                    memberName = member.Value.name;
                }

                if (memberValid)
                {
                    numMemberRemovedEvents++;
                }

                try
                {
                    if (raiseMemberRemovedError)
                    {
                        throw new InvalidOperationException($"Simulated error for member '{memberName}' when calling GenericPresenceChannel.MemberRemoved.");
                    }
                }
                finally
                {
                    if (numMemberRemovedEvents == expectedNumMemberRemovedEvents)
                    {
                        memberRemovedEvent.Set();
                    }
                }
            }

            // Act
            IList<Pusher> pusherMembers = await SubscribeMultipleMembersAsync(connectBeforeSubscribing: connectBeforeSubscribing, numberOfMembers, channelName).ConfigureAwait(false);
            for (int i = 0; i < pusherMembers.Count; i++)
            {
                pusherMembers[i].Error += RemoveMemberErrorHandler;
                var presenceChannel = await pusherMembers[i].SubscribePresenceAsync<FakeUserInfo>(channelName).ConfigureAwait(false);
                presenceChannel.MemberRemoved += MemberRemovedEventHandler;
            }

            await pusherMembers[0].DisconnectAsync().ConfigureAwait(false);

            // Assert
            Assert.IsTrue(memberRemovedEvent.WaitOne(TimeSpan.FromSeconds(7)), $"# Member removed events = {numMemberRemovedEvents}, expected {expectedNumMemberRemovedEvents}");
            if (raiseMemberRemovedError)
            {
                Assert.IsTrue(memberRemovedErrorEvent.WaitOne(TimeSpan.FromSeconds(5)));
            }

            Assert.AreEqual(expectedNumMemberRemovedErrorEvents, numMemberRemovedErrors, "# MemberRemoved errors");
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

        private static bool ValidateMember(KeyValuePair<string, FakeUserInfo> member)
        {
            bool memberValid = !string.IsNullOrWhiteSpace(member.Key);
            if (memberValid && member.Value == null) memberValid = false;
            if (memberValid && string.IsNullOrWhiteSpace(member.Value.name)) memberValid = false;
            return memberValid;
        }

        private static void ValidateMemberAddedEventHandlerException(PusherException error, AutoResetEvent memberAddedErrorEvent)
        {
            if (error is MemberAddedEventHandlerException<FakeUserInfo> memberAddedException)
            {
                bool errorValid = memberAddedException.InnerException is InvalidOperationException;
                if (errorValid && string.IsNullOrWhiteSpace(memberAddedException.MemberKey)) errorValid = false;
                if (errorValid && memberAddedException.Member == null) errorValid = false;
                if (errorValid && memberAddedException.Member.name == null) errorValid = false;
                if (errorValid && memberAddedException.PusherCode != ErrorCodes.MemberAddedEventHandlerError) errorValid = false;
                if (errorValid)
                {
                    memberAddedErrorEvent.Set();
                }
            }
        }

        private static void ValidateMemberRemovedEventHandlerException(PusherException error, int expectedNumMemberRemovedErrorEvents, int numMemberRemovedErrors, AutoResetEvent memberRemovedErrorEvent)
        {
            if (error is MemberRemovedEventHandlerException<FakeUserInfo> memberRemovedException)
            {
                bool errorValid = memberRemovedException.InnerException is InvalidOperationException;
                if (errorValid && string.IsNullOrWhiteSpace(memberRemovedException.MemberKey)) errorValid = false;
                if (errorValid && memberRemovedException.Member == null) errorValid = false;
                if (errorValid && memberRemovedException.Member.name == null) errorValid = false;
                if (errorValid && memberRemovedException.PusherCode != ErrorCodes.MemberRemovedEventHandlerError) errorValid = false;
                if (errorValid)
                {
                    if (numMemberRemovedErrors == expectedNumMemberRemovedErrorEvents)
                    {
                        memberRemovedErrorEvent.Set();
                    }
                }
            }
        }

        #endregion
    }
}
