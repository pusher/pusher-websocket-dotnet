using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    public partial class SubscriptionTest
    {
        #region Connect then subscribe tests

        [Test]
        public async Task PresenceChannelConnectThenSubscribeAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelConnectThenSubscribeWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Presence, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelConnectThenSubscribeTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelConnectThenSubscribeMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelConnectThenSubscribeWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            PusherException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeTestAsync(ChannelTypes.Presence, pusher: pusher).ConfigureAwait(false);
            }
            catch (PusherException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("A ChannelAuthorizer needs to be provided when subscribing to the private or presence channel", caughtException.Message);
        }

        #endregion

        #region Subscribe then connect tests

        [Test]
        public async Task PresenceChannelSubscribeThenConnectAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelSubscribeThenConnectWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Presence, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelSubscribeTwiceThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelSubscribeMultipleTimesThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        #endregion

        #region No connection tests

        [Test]
        public async Task PresenceChannelSubscribeWithoutConnectingAsync()
        {
            await SubscribeWithoutConnectingTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        [Test]
        public async Task PresenceChannelSubscribeThenUnsubscribeWithoutConnectingAsync()
        {
            await SubscribeThenUnsubscribeWithoutConnectingTestAsync(ChannelTypes.Presence).ConfigureAwait(false);
        }

        #endregion
    }
}
