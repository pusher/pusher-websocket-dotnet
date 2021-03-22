﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PusherClient.Tests.Utilities;
using WebSocket4Net;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class ConnectionTest
    {
        private readonly List<Pusher> _clients = new List<Pusher>(10);

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectWhenGivenAValidAppKeyAsync()
        {
            Pusher pusher = await ConnectTestAsync().ConfigureAwait(false);
            Assert.IsNotNull(pusher);
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectTwiceWhenGivenAValidAppKeyAsync()
        {
            // Once
            Pusher pusher = await ConnectTestAsync().ConfigureAwait(false);
            Assert.IsNotNull(pusher);

            // Twice
            await pusher.ConnectAsync().ConfigureAwait(false);
            Assert.AreEqual(ConnectionState.Connected, pusher.State);
        }

        [Test]
        public void CallingPusherConnectMultipleTimesShouldBeIdempotent()
        {
            // Arrange
            bool connected = false;
            bool errored = false;
            int connectedCount = 0;
            int stateChangedCount = 0;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            pusher.Connected += sender =>
            {
                connectedCount++;
                connected = true;
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                stateChangeLog.Add(state);
                Trace.TraceInformation($"[{stateChangedCount}]: {state}");
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                Trace.TraceInformation($"[Error]: {error.Message}");
            };

            // Act
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.ConnectAsync();
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(connected, nameof(connected));
            Assert.AreEqual(1, connectedCount, nameof(connectedCount));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Connecting, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.Connected, stateChangeLog[1]);
            Assert.IsFalse(errored, nameof(errored));
        }

        [Test]
        public async Task PusherShouldNotSuccessfullyConnectWhenGivenAnInvalidAppKeyAsync()
        {
            // Arrange
            PusherException exception = null;
            PusherException caughtException = null;
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int expectedFinalCount = 1;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = new Pusher("Invalid");
            pusher.Connected += sender =>
            {
                connected = true;
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangeLog.Add(state);
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                exception = error;
            };

            // Act
            try
            {
                await pusher.ConnectAsync().ConfigureAwait(false);
            }
            catch (PusherException error)
            {
                caughtException = error;
            }

            /*
                Wait for 1.5 seconds to ensure that auto reconnect does not happen.
                The value of disconnected would be true if auto reconnect occured and 
                there would be more than a single state change.
            */
            await Task.Delay(1500).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Connecting, pusher.State, nameof(pusher.State));
            Assert.IsFalse(connected, nameof(connected));
            Assert.IsFalse(disconnected, nameof(disconnected));
            Assert.IsTrue(errored, nameof(errored));
            Assert.IsNotNull(exception);
            Assert.IsNotNull(caughtException);
            Assert.AreEqual(exception.Message, caughtException.Message);
            Assert.AreEqual(ErrorCodes.ApplicationDoesNotExist, exception.PusherCode);
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Connecting, stateChangeLog[0]);
        }

        [Test]
        public async Task PusherShouldSuccessfullyDisconnectEvenIfNotConnectedAsync()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(saveTo: _clients);

            // Act
            await pusher.DisconnectAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Uninitialized, pusher.State, nameof(pusher.State));
        }

        [Test]
        public async Task PusherShouldSuccessfullyDisconnectWhenItIsConnectedAsync()
        {
            Pusher pusher = await DisconnectTestAsync().ConfigureAwait(false);
            Assert.IsNotNull(pusher);
        }

        [Test]
        public async Task CallingPusherDisconnectMultipleTimesShouldBeIdempotentAsync()
        {
            // Arrange
            var disconnectedEvent = new AutoResetEvent(false);
            var statusChangeEvent = new AutoResetEvent(false);
            bool disconnected = false;
            bool errored = false;
            int disconnectedCount = 0;
            int stateChangedCount = 0;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = await ConnectTestAsync().ConfigureAwait(false);
            pusher.Disconnected += sender =>
            {
                disconnectedCount++;
                disconnected = true;
                disconnectedEvent.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                stateChangeLog.Add(state);
                Trace.TraceInformation($"[{stateChangedCount}]: {state}");
                if (stateChangedCount == expectedFinalCount)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
                Trace.TraceInformation($"[Error]: {error.Message}");
            };

            // Act
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.DisconnectAsync();
                }));
            };

            Task.WaitAll(tasks.ToArray());
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(ConnectionState.Disconnected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(disconnected, nameof(disconnected));
            Assert.AreEqual(1, disconnectedCount, nameof(disconnectedCount));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Disconnecting, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.Disconnected, stateChangeLog[1]);
            Assert.IsFalse(errored, nameof(errored));
        }

        [Test]
        public async Task PusherShouldSuccessfullyReconnectWhenItHasPreviouslyDisconnectedAsync()
        {
            // Arrange
            var connectedEvent = new AutoResetEvent(false);
            var statusChangeEvent = new AutoResetEvent(false);
            bool connected = false;
            bool errored = false;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = await DisconnectTestAsync().ConfigureAwait(false);
            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangeLog.Add(state);
                if (stateChangeLog.Count == expectedFinalCount)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Connected += sender =>
            {
                connected = true;
                connectedEvent.Set();
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
            };

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
            Assert.IsFalse(errored, nameof(errored));
            Assert.IsTrue(connected, nameof(connected));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Connecting, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.Connected, stateChangeLog[1]);
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectEvenWithAConnectingErrorAsync()
        {
            // Arrange
            SortedSet<ConnectionState> raiseErrorOn = new SortedSet<ConnectionState>
            {
                ConnectionState.Connecting,
            };

            // Act and Assert
            await ConnectTestAsync(raiseErrorOn).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectEvenWithAConnectedErrorAsync()
        {
            // Arrange
            SortedSet<ConnectionState> raiseErrorOn = new SortedSet<ConnectionState>
            {
                ConnectionState.Connected,
            };

            // Act and Assert
            await ConnectTestAsync(raiseErrorOn).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherShouldSuccessfullyDisconnectEvenWithADisconnectingErrorAsync()
        {
            // Arrange
            SortedSet<ConnectionState> raiseErrorOn = new SortedSet<ConnectionState>
            {
                ConnectionState.Disconnecting,
            };

            // Act and Assert
            await DisconnectTestAsync(raiseErrorOn).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherShouldSuccessfullyDisconnectEvenWithADisconnectedErrorAsync()
        {
            // Arrange
            SortedSet<ConnectionState> raiseErrorOn = new SortedSet<ConnectionState>
            {
                ConnectionState.Disconnected,
            };

            // Act and Assert
            await DisconnectTestAsync(raiseErrorOn).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherShouldSuccessfullyReconnectWhenTheUnderlyingSocketIsClosedAsync()
        {
            // Arrange
            AutoResetEvent connectedEvent = new AutoResetEvent(false);
            AutoResetEvent disconnectedEvent = new AutoResetEvent(false);
            AutoResetEvent statusChangeEvent = new AutoResetEvent(false);
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int stateChangedCount = 0;
            int expectedFinalCount = 4;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);
            var pusher = await ConnectTestAsync().ConfigureAwait(false);

            pusher.Connected += sender =>
            {
                connected = true;
                connectedEvent.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                stateChangeLog.Add(state);
                if (stateChangedCount == expectedFinalCount)
                {
                    statusChangeEvent.Set();
                }
            };

            pusher.Disconnected += sender =>
            {
                disconnected = true;
                disconnectedEvent.Set();
            };

            pusher.Error += (sender, error) =>
            {
                errored = true;
            };

            // Act
            await Task.Run(() =>
            {
                WebSocket socket = GetWebSocket(pusher);
                socket.Close();
            }).ConfigureAwait(false);
            disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            connectedEvent.WaitOne(TimeSpan.FromSeconds(5));
            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(connected, nameof(connected));
            Assert.IsTrue(disconnected, nameof(disconnected));
            Assert.IsFalse(errored, nameof(errored));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Disconnected, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.WaitingToReconnect, stateChangeLog[1]);
            Assert.AreEqual(ConnectionState.Connecting, stateChangeLog[2]);
            Assert.AreEqual(ConnectionState.Connected, stateChangeLog[3]);
        }

        private static int CalculateExpectedErrorCount(SortedSet<ConnectionState> raiseErrorOn)
        {
            int expectedErrorCount = 0;
            if (raiseErrorOn != null)
            {
                foreach (var status in raiseErrorOn)
                {
                    switch (status)
                    {
                        case ConnectionState.WaitingToReconnect:
                        case ConnectionState.Connecting:
                        case ConnectionState.Disconnecting:
                            expectedErrorCount++;
                            break;
                        case ConnectionState.Connected:
                        case ConnectionState.Disconnected:
                            expectedErrorCount += 2;
                            break;
                    }
                }
            }

            return expectedErrorCount;
        }

        private async Task<Pusher> ConnectTestAsync(SortedSet<ConnectionState> raiseErrorOn = null)
        {
            // Arrange
            var connectedEvent = new AutoResetEvent(false);
            var statusChangeEvent = new AutoResetEvent(false);
            var errorEvent = raiseErrorOn == null ? null : new AutoResetEvent(false);
            int errorCount = 0;
            int expectedErrorCount = CalculateExpectedErrorCount(raiseErrorOn); ;
            int stateChangedCount = 0;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            Assert.IsNull(pusher.SocketID);
            pusher.Connected += sender =>
            {
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(ConnectionState.Connected))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(pusher.Connected)}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    connectedEvent.Set();
                }
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                stateChangeLog.Add(state);
                Trace.TraceInformation($"[{stateChangedCount}]: {state}");
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(state))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(pusher.ConnectionStateChanged)} for state change {state}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    if (stateChangedCount == expectedFinalCount)
                    {
                        statusChangeEvent.Set();
                    }
                }
            };

            pusher.Error += (sender, error) =>
            {
                errorCount++;
                Trace.TraceInformation($"[Error]: {error.Message}");
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Count > 0)
                    {
                        string errorMsg = $"Error ({error.PusherCode}) raised in delegate {nameof(pusher.Error)}. Inner error is:{Environment.NewLine}{error.Message}";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    if (errorCount == expectedErrorCount)
                    {
                        errorEvent?.Set();
                    }
                }
            };

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(connectedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent?.WaitOne(TimeSpan.FromSeconds(5));

            Assert.IsNotNull(pusher.SocketID);
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Connecting, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.Connected, stateChangeLog[1]);
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors");

            return pusher;
        }

        private async Task<Pusher> DisconnectTestAsync(SortedSet<ConnectionState> raiseErrorOn = null)
        {
            // Arrange
            var disconnectedEvent = new AutoResetEvent(false);
            var statusChangeEvent = new AutoResetEvent(false);
            var errorEvent = raiseErrorOn == null ? null : new AutoResetEvent(false);
            int errorCount = 0;
            int expectedErrorCount = CalculateExpectedErrorCount(raiseErrorOn); ;
            int stateChangedCount = 0;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);

            var pusher = await ConnectTestAsync().ConfigureAwait(false);
            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                stateChangeLog.Add(state);
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(state))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(pusher.ConnectionStateChanged)} for state change {state}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    if (stateChangedCount == expectedFinalCount)
                    {
                        statusChangeEvent.Set();
                    }
                }
            };

            pusher.Disconnected += sender =>
            {
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(ConnectionState.Disconnected))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(pusher.Disconnected)}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    disconnectedEvent.Set();
                }
            };

            pusher.Error += (sender, error) =>
            {
                errorCount++;
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Count > 0)
                    {
                        string errorMsg = $"Error ({error.PusherCode}) raised in delegate {nameof(pusher.Error)}. Inner error is:{Environment.NewLine}{error.Message}";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    if (errorCount == expectedErrorCount)
                    {
                        errorEvent?.Set();
                    }
                }
            };

            // Act
            await pusher.DisconnectAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Disconnected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent?.WaitOne(TimeSpan.FromSeconds(5));

            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.AreEqual(ConnectionState.Disconnecting, stateChangeLog[0]);
            Assert.AreEqual(ConnectionState.Disconnected, stateChangeLog[1]);
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors");

            return pusher;
        }

        internal static WebSocket GetWebSocket(Pusher pusher)
        {
            var connection = pusher.GetType().GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(connection);
            var websocket = connection.GetValue(pusher).GetType().GetField("_websocket", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = websocket.GetValue(connection.GetValue(pusher));
            Assert.IsNotNull(result);
            return result as WebSocket;
        }
    }
}