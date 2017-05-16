using System;
using System.Threading;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Subscription
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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

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
                Authorizer = new FakeAuthoriser(UserNameFactory.CreateUniqueUserName())
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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(privateChannel : true);

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
                Authorizer = new FakeAuthoriser(UserNameFactory.CreateUniqueUserName())
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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(presenceChannel: true);

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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            var channelSubscribed = false;
            var numberOfCalls = 0;

            var firstChannel = pusher.Subscribe(mockChannelName);
            firstChannel.Subscribed += sender =>
            {
                channelSubscribed = true;
                numberOfCalls++;
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            var secondChannel = pusher.Subscribe(mockChannelName);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(channelSubscribed);
            Assert.AreEqual(1, numberOfCalls);
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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            // Act
            var firstChannel = pusher.Subscribe(mockChannelName);
            var secondChannel = pusher.Subscribe(mockChannelName);

            // Assert
            Assert.AreEqual(firstChannel, secondChannel);
            Assert.IsTrue(false);
        }

        [Test]
        public void PusherShouldUnsubscribeSuccessfullyWhenTheRequestIsMadeViaTheChannel()
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

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                reset.Set();
            };

            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            channel.Unsubscribe();

            // Assert
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsFalse(channel.IsSubscribed);
        }

        [Test]
        public void PusherShouldUnsubscribeSuccessfullyWhenTheRequestIsMadeViaThePusherObject()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();

            var subscribedEvent = new AutoResetEvent(false);
            var unsubscribedEvent = new AutoResetEvent(false);

            subscribedEvent.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            pusher.Connected += sender =>
            {
                unsubscribedEvent.Set();
            };

            pusher.Connect();

            subscribedEvent.Reset();

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                subscribedEvent.Set();
            };
            channel.Unsubscribed += sender =>
            {
                unsubscribedEvent.Set();
            };

            unsubscribedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            pusher.Unsubscribe(mockChannelName);

            // Assert
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsFalse(channel.IsSubscribed);
        }

        [Test]
        public void PusherShouldUnsubscribeAllTheSubscribedChannelsWhenTheConnectionIsDiconnected()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();

            var connectedEvent = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                connectedEvent.Set();
            };

            pusher.Connect();
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            var subscribedEvent1 = new AutoResetEvent(false);
            var unsubscribedEvent1 = new AutoResetEvent(false);
            var subscribedEvent2 = new AutoResetEvent(false);
            var unsubscribedEvent2 = new AutoResetEvent(false);
            var subscribedEvent3 = new AutoResetEvent(false);
            var unsubscribedEvent3 = new AutoResetEvent(false);

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix : "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = SubscribeToChannel(pusher, mockChannelName1, subscribedEvent1, unsubscribedEvent1);
            var channel2 = SubscribeToChannel(pusher, mockChannelName2, subscribedEvent2, unsubscribedEvent2);
            var channel3 = SubscribeToChannel(pusher, mockChannelName3, subscribedEvent3, unsubscribedEvent3);

            unsubscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            pusher.Disconnect();

            // TODO Wire in the unsubscribe event
            subscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsNotNull(channel1);
            StringAssert.Contains(mockChannelName1, channel1.Name);
            Assert.IsFalse(channel1.IsSubscribed);

            Assert.IsNotNull(channel2);
            StringAssert.Contains(mockChannelName2, channel2.Name);
            Assert.IsFalse(channel2.IsSubscribed);

            Assert.IsNotNull(channel3);
            StringAssert.Contains(mockChannelName3, channel3.Name);
            Assert.IsFalse(channel3.IsSubscribed);
        }

        [Test]
        public void PusherShouldSubscribeAllExistingChannelsWhenTheConnectionIsConnected()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();

            var subscribedEvent1 = new AutoResetEvent(false);
            var unsubscribedEvent1 = new AutoResetEvent(false);
            var subscribedEvent2 = new AutoResetEvent(false);
            var unsubscribedEvent2 = new AutoResetEvent(false);
            var subscribedEvent3 = new AutoResetEvent(false);
            var unsubscribedEvent3 = new AutoResetEvent(false);

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = SubscribeToChannel(pusher, mockChannelName1, subscribedEvent1, unsubscribedEvent1);
            var channel2 = SubscribeToChannel(pusher, mockChannelName2, subscribedEvent2, unsubscribedEvent2);
            var channel3 = SubscribeToChannel(pusher, mockChannelName3, subscribedEvent3, unsubscribedEvent3);

            var connectedEvent = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                connectedEvent.Set();
            };

            // Act
            pusher.Connect();
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            subscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsNotNull(channel1);
            StringAssert.Contains(mockChannelName1, channel1.Name);
            Assert.IsTrue(channel1.IsSubscribed);

            Assert.IsNotNull(channel2);
            StringAssert.Contains(mockChannelName2, channel2.Name);
            Assert.IsTrue(channel2.IsSubscribed);

            Assert.IsNotNull(channel3);
            StringAssert.Contains(mockChannelName3, channel3.Name);
            Assert.IsTrue(channel3.IsSubscribed);
        }

        [Test]
        public void PusherShouldSubscribeAllPreviouslySubscribedChannelsWhenTheConnectionIsReconnected()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher();

            var connectedEvent = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                connectedEvent.Set();
            };

            pusher.Connect();
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            var subscribedEvent1 = new AutoResetEvent(false);
            var unsubscribedEvent1 = new AutoResetEvent(false);
            var subscribedEvent2 = new AutoResetEvent(false);
            var unsubscribedEvent2 = new AutoResetEvent(false);
            var subscribedEvent3 = new AutoResetEvent(false);
            var unsubscribedEvent3 = new AutoResetEvent(false);

            var mockChannelName1 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "1");
            var mockChannelName2 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "2");
            var mockChannelName3 = ChannelNameFactory.CreateUniqueChannelName(channelNamePostfix: "3");

            var channel1 = SubscribeToChannel(pusher, mockChannelName1, subscribedEvent1, unsubscribedEvent1);
            var channel2 = SubscribeToChannel(pusher, mockChannelName2, subscribedEvent2, unsubscribedEvent2);
            var channel3 = SubscribeToChannel(pusher, mockChannelName3, subscribedEvent3, unsubscribedEvent3);

            unsubscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            pusher.Disconnect();

            // TODO Wire in the unsubscribe event
            subscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));
            unsubscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            subscribedEvent1.Reset();
            subscribedEvent2.Reset();
            subscribedEvent3.Reset();

            subscribedEvent1.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent2.WaitOne(TimeSpan.FromSeconds(5));
            subscribedEvent3.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            pusher.Connect();

            // Assert
            Assert.IsNotNull(channel1);
            StringAssert.Contains(mockChannelName1, channel1.Name);
            Assert.IsTrue(channel1.IsSubscribed);

            Assert.IsNotNull(channel2);
            StringAssert.Contains(mockChannelName2, channel2.Name);
            Assert.IsTrue(channel2.IsSubscribed);

            Assert.IsNotNull(channel3);
            StringAssert.Contains(mockChannelName3, channel3.Name);
            Assert.IsTrue(channel3.IsSubscribed);
        }

        private static Channel SubscribeToChannel(Pusher pusher, string mockChannelName1, AutoResetEvent subscribedEvent, AutoResetEvent unsubscribedEvent = null)
        {
            var channel1 = pusher.Subscribe(mockChannelName1);
            channel1.Subscribed += sender => { subscribedEvent.Set(); };
            channel1.Unsubscribed += sender => { unsubscribedEvent.Set(); };
            return channel1;
        }
    }
}
