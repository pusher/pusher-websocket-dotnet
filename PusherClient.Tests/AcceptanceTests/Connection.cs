using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Connection
    {
        [Test]
        public void PusherShouldSuccessfullyConnectWhenGivenAValidAppKey()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            AutoResetEvent statusChangeEvent = new AutoResetEvent(false);
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int stateChangeCount = 0;
            ConnectionState actualState = ConnectionState.Uninitialized;
            ConnectionState expectedState = ConnectionState.Connected;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangeCount++;
                actualState = state;
                if (state == expectedState)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                reset.Set();
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                reset.Set();
            };

            // Act
            Task.Run(() => pusher.ConnectAsync());
            reset.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(connected, nameof(connected));
            Assert.IsFalse(disconnected, nameof(disconnected));
            Assert.IsFalse(errored, nameof(errored));
            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            Assert.AreEqual(expectedState, actualState);
            Assert.AreEqual(2, stateChangeCount, nameof(stateChangeCount));
        }

        [Test]
        public void PusherShouldNotSuccessfullyConnectWhenGivenAnInvalidAppKey()
        {
            // Arrange
            AutoResetEvent reset = new AutoResetEvent(false);
            AutoResetEvent statusChangeEvent = new AutoResetEvent(false);
            PusherException exception = null;
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int stateChangeCount = 0;
            ConnectionState actualState = ConnectionState.Uninitialized;
            ConnectionState expectedState = ConnectionState.Connecting;

            var pusher = new Pusher("Invalid");
            pusher.Connected += sender =>
            {
                connected = true;
                reset.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangeCount++;
                actualState = state;
                if (state == expectedState)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                reset.Set();
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                exception = error;
                reset.Set();
            };

            // Act
            Task.Run(() => pusher.ConnectAsync());

            // Assert
            Assert.IsTrue(reset.WaitOne(TimeSpan.FromSeconds(5)), nameof(reset));
            Assert.IsFalse(connected, nameof(connected));
            Assert.IsFalse(disconnected, nameof(disconnected));
            Assert.IsTrue(errored, nameof(errored));
            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorCodes.ApplicationDoesNotExist, exception.PusherCode);
            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            Assert.AreEqual(expectedState, actualState);
            Assert.AreEqual(1, stateChangeCount, nameof(stateChangeCount));
        }

        [Test]
        public void PusherShouldSuccessfullyDisconnectWhenItIsConnectedAndDisconnectIsRequested()
        {
            // Arrange
            AutoResetEvent connectedEvent = new AutoResetEvent(false);
            AutoResetEvent disconnectedEvent = new AutoResetEvent(false);
            AutoResetEvent statusChangeEvent = new AutoResetEvent(false);
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int stateChangeCount = 0;
            ConnectionState actualState = ConnectionState.Uninitialized;
            ConnectionState expectedState = ConnectionState.Disconnected;

            var pusher = PusherFactory.GetPusher();
            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangeCount++;
                actualState = state;
                if (state == expectedState)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Connected += sender =>
            {
                connected = true;
                connectedEvent.Set();
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                disconnectedEvent.Set();
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                connectedEvent.Set();
                disconnectedEvent.Set();
            };

            // Act
            Task.Run(() => pusher.ConnectAsync());
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            Task.Run(() => pusher.DisconnectAsync());

            // Assert
            Assert.IsTrue(disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5)), "Disconnected event set");
            Assert.IsTrue(connected, nameof(connected));
            Assert.IsTrue(disconnected, nameof(disconnected));
            Assert.IsFalse(errored, nameof(errored));
            Assert.IsTrue(statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5)), nameof(statusChangeEvent));
            Assert.AreEqual(expectedState, actualState);
            Assert.AreEqual(4, stateChangeCount, nameof(stateChangeCount));
        }

        [Test]
        public void PusherShouldSuccessfullyReconnectWhenItHasPreviouslyDisconnected()
        {
            // Arrange
            AutoResetEvent connectedEvent = new AutoResetEvent(false);
            AutoResetEvent disconnectedEvent = new AutoResetEvent(false);
            bool disconnected = false;
            bool errored = false;
            var connects = 0;

            var pusher = PusherFactory.GetPusher();
            pusher.Connected += sender =>
            {
                connects++;
                connectedEvent.Set();
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                disconnectedEvent.Set();
            };

            // Act
            Task.Run(() => pusher.ConnectAsync());
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            connectedEvent.Reset();
            Task.Run(() => pusher.DisconnectAsync());
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            Task.Run(() => pusher.ConnectAsync());
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(2, connects);
            Assert.IsTrue(disconnected, nameof(disconnected));
            Assert.IsFalse(errored, nameof(errored));
        }
    }
}