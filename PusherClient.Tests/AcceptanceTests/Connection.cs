using System;
using System.Threading;
using Nito.AsyncEx;
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
        public void PusherShouldSuccessfulyConnectWhenGivenAValidAppKeyAsync()
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
            AsyncContext.Run(() => pusher.ConnectAsync());
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
            var connectionResult = pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsFalse(connected);
        }

        [Test]
        public void PusherShouldNotSuccessfulyConnectWhenGivenAnInvalidAppKeyAsync()
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
            var connectionResult = AsyncContext.Run(() => pusher.ConnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsFalse(connected);
        }

        [Test]
        public void PusherShouldSuccessfulyDisconnectWhenItIsConnectedAndDisconnectIsRequested()
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
        public void PusherShouldSuccessfulyDisconnectWhenItIsConnectedAndDisconnectIsRequestedAsync()
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
            AsyncContext.Run(() => pusher.ConnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));
            AsyncContext.Run(() => pusher.DisconnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(connected);
            Assert.IsTrue(disconnected);
        }

        [Test]
        public void PusherShouldNotSuccessfullyDisconnectWhenItIsNotDisconnected()
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
        public void PusherShouldNotSuccessfullyDisconnectWhenItIsNotDisconnectedAsync()
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
            AsyncContext.Run(() => pusher.DisconnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsFalse(connected);
            Assert.IsFalse(disconnected);
        }

        [Test]
        public void PusherShouldSuccessfulyReconnectWhenItHasPreviouslyDisconnected()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            var connects = 0;
            var disconnected = false;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connects++;
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
            pusher.Connect();
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(2, connects);
            Assert.IsTrue(disconnected);
        }

        [Test]
        public void PusherShouldSuccessfulyReconnectWhenItHasPreviouslyDisconnectedAsync()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            var connects = 0;
            var disconnected = false;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connects++;
                reset.Set();
            };

            reset.Reset();

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                reset.Set();
            };

            // Act
            AsyncContext.Run(() => pusher.ConnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));
            AsyncContext.Run(() => pusher.DisconnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));
            AsyncContext.Run(() => pusher.ConnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(2, connects);
            Assert.IsTrue(disconnected);
        }

        // TODO - Multi threading tests around connection
        //http://blog.jerometerry.com/2014/06/multi-threaded-nunit-tests.html

        // TODO - Error handling in connection, prove with unit tests?
    }
}