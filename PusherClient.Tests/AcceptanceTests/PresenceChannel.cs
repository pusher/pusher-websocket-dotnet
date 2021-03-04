using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class PresenceChannel
    {
        [Test]
        public async Task PresenceChannelShouldAddAMemberWhenGivenAMemberAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            AutoResetEvent reset = new AutoResetEvent(false);

            await pusher.ConnectAsync();
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(presenceChannel: true);
            string subscribedChannelName = null;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedChannelName = channelName;
                channelSubscribed = true;
                reset.Set();
            };

            // Act
            var channel = await pusher.SubscribeAsync(mockChannelName);

            reset.WaitOne(TimeSpan.FromSeconds(10));

            // Assert
            ValidateChannel(pusher, mockChannelName, channel);
            Assert.AreEqual(mockChannelName, subscribedChannelName);
            Assert.IsTrue(channelSubscribed);
        }

        [Test]
        public async Task PresenceChannelShouldAddATypedMemberWhenGivenAMemberAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            AutoResetEvent reset = new AutoResetEvent(false);

            await pusher.ConnectAsync();
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(presenceChannel: true);
            string subscribedChannelName = null;
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                subscribedChannelName = channelName;
                channelSubscribed = true;
                reset.Set();
            };

            // Act
            var channel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);

            reset.WaitOne(TimeSpan.FromSeconds(10));

            // Assert
            ValidateChannel(pusher, mockChannelName, channel);

            CollectionAssert.IsNotEmpty(channel.Members);
            Assert.AreEqual(mockChannelName, subscribedChannelName);
            Assert.IsTrue(channelSubscribed);
        }

        private static void ValidateChannel(Pusher pusher, string mockChannelName, Channel channel)
        {
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsTrue(channel.IsSubscribed);

            // Validate GetChannelInfoList results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsTrue(channels != null && channels.Count == 1);
            Assert.AreEqual(channel.Name, channels[0].Name, nameof(Channel.Name));
            Assert.AreEqual(true, channels[0].IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(ChannelTypes.Presence, channels[0].ChannelType, nameof(Channel.ChannelType));
        }
    }
}
