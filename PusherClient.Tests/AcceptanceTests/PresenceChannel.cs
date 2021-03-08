using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class PresenceChannel
    {
        [Test]
        public async Task PresenceChannelShouldAddATypedMemberWhenGivenAMemberAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()));
            await pusher.ConnectAsync().ConfigureAwait(false);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(channelType: ChannelTypes.Presence);
            var channelSubscribed = false;
            pusher.Subscribed += (sender, channelName) =>
            {
                if (channelName == mockChannelName)
                {
                    channelSubscribed = true;
                }
            };

            // Act
            var channel = await pusher.SubscribePresenceAsync<FakeUserInfo>(mockChannelName).ConfigureAwait(false);

            // Assert
            ValidateChannel(pusher, mockChannelName, channel);

            CollectionAssert.IsNotEmpty(channel.Members);
            Assert.IsTrue(channelSubscribed);
        }

        private static void ValidateChannel(Pusher pusher, string mockChannelName, Channel channel)
        {
            Assert.IsNotNull(channel);
            StringAssert.Contains(mockChannelName, channel.Name);
            Assert.IsTrue(channel.IsSubscribed);

            // Validate GetChannelInfoList results
            IList<Channel> channels = pusher.GetAllChannels();
            Assert.IsNotNull(channels);
            Assert.IsTrue(channels.Count >= 1);
            Assert.AreEqual(channel.Name, channels[0].Name, nameof(Channel.Name));
            Assert.AreEqual(true, channels[0].IsSubscribed, nameof(Channel.IsSubscribed));
            Assert.AreEqual(ChannelTypes.Presence, channels[0].ChannelType, nameof(Channel.ChannelType));
        }
    }
}
