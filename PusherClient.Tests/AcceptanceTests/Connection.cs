using System;
using System.Threading;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class PusherIntegrationTests
    {
        [Test]
        public void PusherShouldSuccessfulyConnectWhenGivenAValidAppKey()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            bool connected = false;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            // Act
            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(connected);
        }

        [Test]
        public void PusherShouldNotSuccessfulyConnectWhenGivenAnInvalidAppKey()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            bool connected = false;

            var pusher = new Pusher("Invalid");
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            // Act
            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsFalse(connected);
        }

        [Test]
        public void PusherShouldSuccessfulyDisconnectWhenItIsConnectedAndDIsconnectIsRequested()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            bool connected = false;
            bool disconnected = false;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            reset.Reset();

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                reset.Set();
            };

            // Act
            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));
            pusher.Disconnect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(connected);
            Assert.IsTrue(disconnected);
        }

        [Test]
        public void PusherShouldNotSuccessfulyDisconnectWhenItIsNotDisconnected()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            bool connected = false;
            bool disconnected = false;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                reset.Set();
            };

            // Act
            pusher.Disconnect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsFalse(connected);
            Assert.IsFalse(disconnected);
        }

        // TODO - Multi threading tests around connection

        // TODO - Error handling in connection, prove with unit tests?
    }
}