using System;
using System.Threading;
using Nito.AsyncEx;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class PresenceChannel
    {
        [Test]
        public void PresenceChannelShouldAddAMemberWhenGivenAMember()
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
        public void PresenceChannelShouldAddAMemberWhenGivenAMemberAsync()
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
            var channel = AsyncContext.Run(() => pusher.SubscribeAsync(mockChannelName));
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
    }
}
