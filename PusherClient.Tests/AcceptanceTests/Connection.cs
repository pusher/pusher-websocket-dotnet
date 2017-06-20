using System;
using System.Threading;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Connection
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
        public void PusherAsyncShouldSuccessfulyConnectWhenGivenAValidAppKey()
        {
            // Arrange
            var pusher = PusherFactory.GetPusherAsync();

            // Act
            var connectionState = pusher.Connect().Result;

            // Assert
            Assert.AreEqual(PusherAsync.AsyncConnectionState.Connected, connectionState);
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
        public void PusherAsyncShouldNotSuccessfulyConnectWhenGivenAnInvalidAppKey()
        {
            // Arrange
            var pusher = new PusherAsync("Invalid");

            // Act
            var connectionState = pusher.Connect().Result;

            // Assert
            Assert.AreEqual(PusherAsync.AsyncConnectionState.ConnectionFailed, connectionState);
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
        public void PusherAsyncShouldSuccessfulyDisconnectWhenItIsConnectedAndDIsconnectIsRequested()
        {
            // Arrange
            var pusher = PusherFactory.GetPusherAsync();
            var connectionResult = pusher.Connect().Result;

            // Act
            var disconnectionResult = pusher.Disconnect().Result;
            
            // Assert
            Assert.AreEqual(PusherAsync.AsyncConnectionState.Connected, connectionResult);
            Assert.AreEqual(PusherAsync.AsyncConnectionState.Disconnected, disconnectionResult);
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

        [Test]
        public void PusherAsyncShouldNotSuccessfulyDisconnectWhenItIsNotDisconnected()
        {
            // Arrange
            var pusher = PusherFactory.GetPusherAsync();

            // Act
            var disconnectionResult = pusher.Disconnect().Result;

            // Assert
            Assert.AreEqual(PusherAsync.AsyncConnectionState.NotConnected, disconnectionResult);
        }

        // TODO - Multi threading tests around connection
        //http://blog.jerometerry.com/2014/06/multi-threaded-nunit-tests.html

        // TODO - Error handling in connection, prove with unit tests?
    }
}