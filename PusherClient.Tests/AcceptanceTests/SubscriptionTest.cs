using System.Collections.Generic;
using System.Linq;
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
    }
}
