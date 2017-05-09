using System;
using System.Threading;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Subscriptions
    {
        [Test]
        public void PusherShouldSubscribeToAChannelWhenGivenAPopulatedPublicChannelName()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                reset.Set();
            };

            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            reset.Reset();

            var mockChannelName = CreateUniqueChannelName();

            var channelSubscribed = false;

            // Act
            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                channelSubscribed = true;
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsTrue(channel.IsSubscribed);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public void PusherShouldSubscribeToAChannelWhenGivenAPopulatedPrivateChannelName()
        {
            // Arrange
            var stubOptions = new PusherOptions
            {
                Authorizer = new FakeAuthoriser(CreateUniqueUserName())
            };

            var pusher = PusherFactory.GetPusher(stubOptions);
            AutoResetEvent reset = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                reset.Set();
            };

            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            reset.Reset();

            var mockChannelName = CreateUniqueChannelName(privateChannel : true);

            var channelSubscribed = false;

            // Act
            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                channelSubscribed = true;
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsTrue(channel.IsSubscribed);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public void PusherShouldSubscribeToAChannelWhenGivenAPopulatedPresenceChannelName()
        {
            // Arrange
            var stubOptions = new PusherOptions
            {
                Authorizer = new FakeAuthoriser(CreateUniqueUserName())
            };

            var pusher = PusherFactory.GetPusher(stubOptions);
            AutoResetEvent reset = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                reset.Set();
            };

            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            reset.Reset();

            var mockChannelName = CreateUniqueChannelName(presenceChannel: true);

            var channelSubscribed = false;

            // Act
            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                channelSubscribed = true;
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(10));

            // Assert
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsTrue(channel.IsSubscribed);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public void PusherShouldNotCreateAnotherSubscriptionToAChannelIfTheChannelHasAlreadyBeenSubscribedTo()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                reset.Set();
            };

            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            reset.Reset();

            var mockChannelName = CreateUniqueChannelName();

            var channelSubscribed = false;

            var firstChannel = pusher.Subscribe(mockChannelName);
            firstChannel.Subscribed += sender =>
            {
                channelSubscribed = true;
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            var secondChannel = pusher.Subscribe(mockChannelName);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
        }

        [Test]
        public void PusherShouldNotAttemptASecondChannelSubscriptionToAnExistingChannelWhileTheFirstRequestIsWaitingForAResponse()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            AutoResetEvent reset = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                reset.Set();
            };

            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            reset.Reset();

            var mockChannelName = CreateUniqueChannelName();

            // Act
            var firstChannel = pusher.Subscribe(mockChannelName);
            var secondChannel = pusher.Subscribe(mockChannelName);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(false);
        }

        [Test]
        public void PusherShouldNotAttemptToSubscribeToAChannelWhenNoActiveConnectionExists()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();
            var mockChannelName = CreateUniqueChannelName();

            // Act
            var channel = pusher.Subscribe(mockChannelName);

            // Assert
            Assert.IsNull(channel);
        }

        private static string CreateUniqueChannelName(bool privateChannel = false, bool presenceChannel = false)
        {
            var channelPrefix = string.Empty;

            if (privateChannel)
                channelPrefix = "PRIVATE-";
            else if (presenceChannel)
                channelPrefix = "PRESENCE-";

            var mockChannelName = $"{channelPrefix}myTestChannel{DateTime.Now.Ticks}";
            return mockChannelName;
        }

        private static string CreateUniqueUserName()
        {
            return $"testUser{DateTime.Now.Ticks}";
        }
    }
}
