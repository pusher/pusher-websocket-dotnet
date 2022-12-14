using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    public partial class SubscriptionTest
    {
        #region Connect then subscribe tests

        [Test]
        public async Task PrivateChannelConnectThenSubscribeAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelConnectThenSubscribeWithSubscribedErrorAsync()
        {
            await ConnectThenSubscribeTestAsync(ChannelTypes.Private, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelConnectThenSubscribeTwiceAsync()
        {
            await ConnectThenSubscribeSameChannelTwiceAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelConnectThenSubscribeMultipleTimesAsync()
        {
            await ConnectThenSubscribeSameChannelMultipleTimesTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelConnectThenSubscribeWithoutAuthorizerAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            PusherException caughtException = null;

            // Act
            try
            {
                await ConnectThenSubscribeTestAsync(ChannelTypes.Private, pusher: pusher).ConfigureAwait(false);
            }
            catch (PusherException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("An ChannelAuthorizer needs to be provided when subscribing to the private or presence channel", caughtException.Message);
        }

        #endregion

        #region Subscribe then connect tests

        [Test]
        public async Task PrivateChannelSubscribeThenConnectAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelSubscribeThenConnectWithSubscribedErrorAsync()
        {
            await SubscribeThenConnectTestAsync(ChannelTypes.Private, raiseSubscribedError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelSubscribeTwiceThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelTwiceAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelSubscribeMultipleTimesThenConnectAsync()
        {
            await SubscribeThenConnectSameChannelMultipleTimesTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        #endregion

        #region No connection tests

        [Test]
        public async Task PrivateChannelSubscribeWithoutConnectingAsync()
        {
            await SubscribeWithoutConnectingTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        [Test]
        public async Task PrivateChannelSubscribeThenUnsubscribeWithoutConnectingAsync()
        {
            await SubscribeThenUnsubscribeWithoutConnectingTestAsync(ChannelTypes.Private).ConfigureAwait(false);
        }

        #endregion
    }
}
