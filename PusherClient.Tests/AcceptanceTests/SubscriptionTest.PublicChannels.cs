using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests subscribe and unsubscribe functionality for Public channels.
    /// </summary>
    [TestFixture]
    public partial class SubscriptionTest
    {
        #region Connect then subscribe tests

        [Test]
        public async Task PublicChannelConnectThenSubscribeAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelConnectThenSubscribeWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Public, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelConnectThenSubscribeTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelConnectThenSubscribeMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelConnectThenSubscribeWithoutAnyEventHandlersAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);

            // Assert
            ValidateSubscribedChannel(pusher, mockChannelName, channel, ChannelTypes.Public);
        }

        [Test]
        public async Task PublicChannelUnsubscribeUsingChannelUnsubscribeAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName();

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            var channel = await pusher.SubscribeAsync(mockChannelName).ConfigureAwait(false);
            channel.Unsubscribe();

            // Assert
            ValidateUnsubscribedChannel(pusher, channel);
        }

        #endregion

        #region Subscribe then connect tests

        [Test]
        public async Task PublicChannelSubscribeThenConnectAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelSubscribeThenConnectWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Public, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelSubscribeTwiceThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelSubscribeMultipleTimesThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        #endregion

        #region No connection tests

        [Test]
        public async Task PublicChannelSubscribeWithoutConnectingAsync()
        {
            await SubscribeWithoutConnectingTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicChannelSubscribeThenUnsubscribeWithoutConnectingAsync()
        {
            await SubscribeThenUnsubscribeWithoutConnectingTestAsync(ChannelTypes.Public).ConfigureAwait(false);
        }

        #endregion
    }
}
