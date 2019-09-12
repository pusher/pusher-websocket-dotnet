using System;
using Nito.AsyncEx;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class PusherTests
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
        public void PusherShouldThrowAnExceptionWhenSubscribeIsCalledWithAnEmptyStringForAChannelNameAsync()
        {
            // Arrange
            ArgumentException caughtException = null;

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = AsyncContext.Run(() => pusher.SubscribeAsync(string.Empty));
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The channel name cannot be null or whitespace", caughtException.Message);
        }

        [Test]
        public void PusherShouldThrowAnExceptionWhenSubscribeIsCalledWithANullStringForAChannelNameAsync()
        {
            // Arrange
            ArgumentException caughtException = null;

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = AsyncContext.Run(() => pusher.SubscribeAsync(null));
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The channel name cannot be null or whitespace", caughtException.Message);
        }

        [Test]
        public void PusherShouldThrowAnExceptionWhenSubscribePresenceIsCalledWithANonPresenceChannelAsync()
        {
            // Arrange
            ArgumentException caughtException = null;

            // Act
            try
            {
                var pusher = new Pusher("FakeAppKey");
                var channel = AsyncContext.Run(() => pusher.SubscribePresenceAsync<string>("private-123"));
            }
            catch (ArgumentException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("The channel name must be refer to a presence channel", caughtException.Message);
        }

        [Test]
        public void PusherShouldThrowAnExceptionWhenSubscribePresenceIsCalledWithADifferentTypeAsync()
        {
            // Arrange
            InvalidOperationException caughtException = null;

            // Act
            var pusher = new Pusher("FakeAppKey", new PusherOptions { Authorizer = new FakeAuthoriser("test") });
            AsyncContext.Run(() => pusher.SubscribePresenceAsync<string>("presence-123"));

            try
            {
                AsyncContext.Run(() => pusher.SubscribePresenceAsync<int>("presence-123"));
            }
            catch (InvalidOperationException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            StringAssert.Contains("Cannot change channel member type; was previously defined as", caughtException.Message);
        }
    }
}