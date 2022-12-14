using System;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class PusherTest
    {
        [Test]
        public void PusherShouldThrowAnExceptionWhenInitialisedWithANullApplicationKey()
        {
            // Arrange
            var stubOptions = new PusherOptions();

            ArgumentException caughtException = null;

            // Act
            try
            {
                new Pusher(null, stubOptions);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The application key cannot be null or whitespace", caughtException.Message);
        }

        [Test]
        public void PusherShouldThrowAnExceptionWhenInitialisedWithAnEmptyApplicationKey()
        {
            // Arrange
            var stubOptions = new PusherOptions();

            ArgumentException caughtException = null;

            // Act
            try
            {
                new Pusher(string.Empty, stubOptions);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The application key cannot be null or whitespace", caughtException.Message);
        }

        [Test]
        public void PusherShouldThrowAnExceptionWhenInitialisedWithAWhitespaceApplicationKey()
        {
            // Arrange
            var stubOptions = new PusherOptions();

            ArgumentException caughtException = null;

            // Act
            try
            {
                new Pusher("  ", stubOptions);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The application key cannot be null or whitespace", caughtException.Message);
        }

        [Test]
        public void PusherShouldUseAPassedInPusherOptionsWhenSupplied()
        {
            // Arrange
            var mockOptions = new PusherOptions()
            {
                Cluster = "MyCluster"
            };

            // Act
            var pusher = new Pusher("FakeKey", mockOptions);

            //Assert
            Assert.AreEqual(mockOptions, pusher.Options);
        }

        [Test]
        public void PusherShouldCreateANewPusherOptionsWhenNoPusherOptionsAreSupplied()
        {
            // Arrange

            // Act
            var pusher = new Pusher("FakeKey");

            //Assert
            Assert.IsNotNull(pusher.Options);
        }

        [Test]
        public async Task PusherShouldThrowAnExceptionWhenSubscribeIsCalledWithAnEmptyStringForAChannelNameAsync()
        {
            // Arrange
            ArgumentException caughtException = null;

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = await pusher.SubscribeAsync(string.Empty).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("Value cannot be null.", caughtException.Message);
        }

        [Test]
        public async Task PusherShouldThrowAnExceptionWhenSubscribeIsCalledWithANullStringForAChannelNameAsync()
        {
            // Arrange
            ArgumentException caughtException = null;

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = await pusher.SubscribeAsync(null).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("Value cannot be null.", caughtException.Message);
        }

        [Test]
        public async Task PusherShouldThrowAnExceptionWhenSubscribePresenceIsCalledWithANonPresenceChannelAsync()
        {
            // Arrange
            ArgumentException caughtException = null;
            string channelName = "private-123";

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = await pusher.SubscribePresenceAsync<string>(channelName).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains($"The channel name '{channelName}' is not that of a presence channel.", caughtException.Message);
        }

        [Test]
        public async Task PusherShouldThrowAnExceptionWhenSubscribePresenceIsCalledWithADifferentTypeAsync()
        {
            // Arrange
            ChannelException caughtException = null;

            // Act
            var pusher = new Pusher("FakeAppKey", new PusherOptions { ChannelAuthorizer = new FakeChannelAuthoriser("test") });
            await pusher.SubscribePresenceAsync<string>("presence-123").ConfigureAwait(false);

            try
            {
                await pusher.SubscribePresenceAsync<int>("presence-123").ConfigureAwait(false);
            }
            catch (ChannelException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The presence channel 'presence-123' has already been created but with a different type", caughtException.Message);
        }

        [Test]
        public async Task PusherShouldThrowAnExceptionWhenSubscribePresenceIsCalledAfterSubscribeAsync()
        {
            // Arrange
            ChannelException caughtException = null;

            // Act
            var pusher = new Pusher("FakeAppKey", new PusherOptions { ChannelAuthorizer = new FakeChannelAuthoriser("test") });
            await pusher.SubscribeAsync("presence-123").ConfigureAwait(false);

            try
            {
                await pusher.SubscribePresenceAsync<int>("presence-123").ConfigureAwait(false);
            }
            catch (ChannelException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The presence channel 'presence-123' has already been created as a PresenceChannel", caughtException.Message);
        }
    }
}