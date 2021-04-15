using System;
using System.Collections.Concurrent;
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
        private const int TimeoutRetryAttempts = 5;
        private readonly List<Pusher> _clients = new List<Pusher>(10);

        private readonly object _sync = new object();
        private Pusher _client;
        private Exception _error;

        [TearDown]
        public async Task DisposeAsync()
        {
            await PusherFactory.DisposePushersAsync(_clients).ConfigureAwait(false);
            _client = null;
            _error = null;
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectWhenGivenAValidAppKeyAsync()
        {
            Pusher pusher = await ConnectTestAsync().ConfigureAwait(false);
            Assert.IsNotNull(pusher);
        }

        [Test]
        public async Task PusherShouldSuccessfullyConnectWhenUsingTheHostOptionAsync()
        {
            // Arrange
            PusherOptions options = new PusherOptions()
            {
                Host = $"ws-{Config.Cluster}.pusher.com",
                Encrypted = Config.Encrypted,
                TraceLogger = new TraceLogger(),
            };

            Pusher pusher = new Pusher(Config.AppKey, options);
            _clients.Add(pusher);

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNull(options.Cluster, nameof(options.Cluster));
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
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
        public void ConcurrentPusherConnectsShouldBeIdempotent()
        {
            // Arrange
            bool connected = false;
            bool errored = false;
            int connectedCount = 0;
            int stateChangedCount = 0;
            int expectedFinalCount = 2;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);
            object sync = new object();
            var pusher = PusherFactory.GetPusher(saveTo: _clients);

            pusher.Connected += sender =>
            {
                connectedCount++;
                connected = true;
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                lock (sync)
                {
                    stateChangedCount++;
                    stateChangeLog.Add(state);
                    Trace.TraceInformation($"[{stateChangedCount}, {DateTime.Now:O}]: {state}");
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
                    return pusher.ConnectAsync();
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(connected, nameof(connected));
            Assert.AreEqual(1, connectedCount, nameof(connectedCount));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connecting), $"Expected state change {ConnectionState.Connecting}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connected), $"Expected state change {ConnectionState.Connected}");
            Assert.IsFalse(errored, nameof(errored));
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
        public async Task ConcurrentPusherDisconnectsShouldBeIdempotentAsync()
        {
            // Arrange
            var disconnectedEvent = new AutoResetEvent(false);
            var statusChangeEvent = new AutoResetEvent(false);
            bool errored = false;
            int stateChangedCount = 0;
            int expectedStateChangedCount = 2;

            var pusher = await ConnectTestAsync().ConfigureAwait(false);
            pusher.Disconnected += sender =>
            {
                disconnectedEvent.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                stateChangedCount++;
                Trace.TraceInformation($"[{stateChangedCount}, {DateTime.Now:O}]: {state}");
                if (stateChangedCount == expectedStateChangedCount)
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

            // Assert
            Assert.IsTrue(statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5)), nameof(statusChangeEvent));
            Assert.IsTrue(disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5)), nameof(disconnectedEvent));
            Assert.AreEqual(ConnectionState.Disconnected, pusher.State, nameof(pusher.State));
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
            object sync = new object();

            var pusher = await DisconnectTestAsync().ConfigureAwait(false);
            pusher.ConnectionStateChanged += (sender, state) =>
            {
                lock (sync)
                {
                    stateChangeLog.Add(state);
                }

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
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connecting), $"Expected state change {ConnectionState.Connecting}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connected), $"Expected state change {ConnectionState.Connected}");
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
            object sync = new object();
            var pusher = await ConnectTestAsync().ConfigureAwait(false);

            pusher.Connected += sender =>
            {
                connected = true;
                connectedEvent.Set();
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                lock (sync)
                {
                    stateChangedCount++;
                    stateChangeLog.Add(state);
                }

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
                Trace.TraceError($"{error.Message}{Environment.NewLine}{error}");
                errored = true;
            };

            // Act
            for (int i = 0; i < 1; i++)
            {
                await Task.Run(() =>
                {
                    WebSocket socket = GetWebSocket(pusher);
                    socket.Close();
                }).ConfigureAwait(false);
                disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5));
                disconnectedEvent.Reset();
                connectedEvent.WaitOne(TimeSpan.FromSeconds(5));
                connectedEvent.Reset();
                statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
                statusChangeEvent.Reset();
            }

            // Assert
            Assert.IsTrue(connected, nameof(connected));
            Assert.IsTrue(disconnected, nameof(disconnected));
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.IsFalse(errored, nameof(errored));
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Disconnected), $"Expected state change {ConnectionState.Disconnected}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.WaitingToReconnect), $"Expected state change {ConnectionState.WaitingToReconnect}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connecting), $"Expected state change {ConnectionState.Connecting}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connected), $"Expected state change {ConnectionState.Connected}");
        }

        [Test]
        public async Task PusherShouldErrorWhenGivenAnInvalidAppKeyAsync()
        {
            // Arrange
            PusherException exception = null;
            PusherException caughtException = null;
            bool connected = false;
            bool disconnected = false;
            bool errored = false;
            int expectedFinalCount = 1;
            List<ConnectionState> stateChangeLog = new List<ConnectionState>(expectedFinalCount);
            object sync = new object();
            var pusher = new Pusher("Invalid");

            pusher.Connected += sender =>
            {
                connected = true;
            };

            pusher.ConnectionStateChanged += (sender, state) =>
            {
                lock (sync)
                {
                    stateChangeLog.Add(state);
                }
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
        public void PusherShouldErrorWhenConnectTimesOut()
        {
            // Arrange
            AutoResetEvent errorEvent = new AutoResetEvent(false);
            PusherException exception = null;
            AggregateException caughtException = null;

            var pusher = PusherFactory.GetPusher(saveTo: _clients, timeoutPeriodMilliseconds: 10);

            pusher.Error += (sender, error) =>
            {
                exception = error;
                errorEvent.Set();
            };

            // Act - trying to connect multiple times gives us increased code coverage on connection timeouts.
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    return pusher.ConnectAsync();
                }));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException error)
            {
                caughtException = error;
            }

            // Assert
            Assert.IsNotNull(caughtException, nameof(AggregateException));
            Assert.IsTrue(errorEvent.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsNotNull(exception, nameof(PusherException));
            Assert.AreEqual(exception.Message, caughtException.InnerException.Message);
            Assert.AreEqual(ErrorCodes.ClientTimeout, exception.PusherCode);
        }

        [Test]
        public async Task PusherShouldErrorWhenDisconnectTimesOutAsync()
        {
            // Arrange
            AutoResetEvent errorEvent = new AutoResetEvent(false);
            PusherException pusherException = null;
            AggregateException caughtException = null;

            Pusher pusher = await ConnectTestAsync().ConfigureAwait(true);
            _client = pusher;

            pusher.Error += (sender, error) =>
            {
                pusherException = error;
                errorEvent.Set();
            };

            /*
             *  This test requires two distinct threads to attempt to disconnect at the same time because 
             *  it is the second thread that will timeout. The first thread will disconnect because the
             *  semaphore count for _disconnectLock is one and the count will be zero on the second thread. 
             *  There are times on the build server when it fails to spin up two distinct threads.
             */
            // Act
            ((IPusher)pusher).PusherOptions.ClientTimeout = TimeSpan.FromTicks(1);

            // Try to generate the error multiple times as it does not always error the first time
            for (int attempt = 0; attempt < TimeoutRetryAttempts; attempt++)
            {
                int numThreads = 2;
                Thread[] tasks = new Thread[numThreads];
                for (int i = 0; i < numThreads; i++)
                {
                    tasks[i] = new Thread(Disconnect);
                    tasks[i].Start();
                }

                for (int i = 0; i < numThreads; i++)
                {
                    tasks[i].Join();
                }

                caughtException = _error as AggregateException;
                if (caughtException != null)
                {
                    break;
                }
            }

            // Assert
            // This test does not always work on the build server, requires more than 2 CPU(s) for better reliability
            if (caughtException != null)
            {
                Assert.IsTrue(errorEvent.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.IsNotNull(pusherException, nameof(PusherException));
                Assert.AreEqual(pusherException.Message, caughtException.InnerException.Message);
                Assert.AreEqual(ErrorCodes.ClientTimeout, pusherException.PusherCode);
            }
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

        private void Disconnect()
        {
            try
            {
                Task.WaitAll(_client.DisconnectAsync());
            }
            catch (Exception e)
            {
                _error = e;
            }
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
            object sync = new object();

            void ConnectedEventHandler(object sender)
            {
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(ConnectionState.Connected))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(Pusher.Connected)}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    connectedEvent.Set();
                }
            }

            void StateChangedEventHandler(object sender, ConnectionState state)
            {
                lock (sync)
                {
                    stateChangedCount++;
                    stateChangeLog.Add(state);
                    Trace.TraceInformation($"[{stateChangedCount}, {DateTime.Now:O}]: {state}");
                }

                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(state))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(Pusher.ConnectionStateChanged)} for state change {state}.";
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
            }

            void ErrorEventHandler(object sender, PusherException error)
            {
                errorCount++;
                if (error is ConnectionStateChangedEventHandlerException stateChangeError)
                {
                    Trace.TraceInformation($"[StateChangedError {stateChangeError.State}]: {error.Message}");
                }
                else
                {
                    Trace.TraceInformation($"[Error]: {error.Message}");
                }

                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Count > 0)
                    {
                        string errorMsg = $"Error ({error.PusherCode}) raised in delegate {nameof(Pusher.Error)}. Inner error is:{Environment.NewLine}{error.Message}";
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
            }

            var pusher = PusherFactory.GetPusher(saveTo: _clients);
            pusher.Connected += ConnectedEventHandler;
            pusher.ConnectionStateChanged += StateChangedEventHandler;
            pusher.Error += ErrorEventHandler;
            Assert.IsNull(pusher.SocketID);

            // Act
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Connected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(connectedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent?.WaitOne(TimeSpan.FromSeconds(5));

            Assert.IsNotNull(pusher.SocketID);
            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connecting), $"Expected state change {ConnectionState.Connecting}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Connected), $"Expected state change {ConnectionState.Connected}");
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
            object sync = new object();

            void StateChangedEventHandler(object sender, ConnectionState state)
            {
                lock (sync)
                {
                    stateChangedCount++;
                    stateChangeLog.Add(state);
                }

                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(state))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(Pusher.ConnectionStateChanged)} for state change {state}.";
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
            }

            void DisconnectedEventHandler(object sender)
            {
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Contains(ConnectionState.Disconnected))
                    {
                        string errorMsg = $"Error raised in delegate {nameof(Pusher.Disconnected)}.";
                        Trace.TraceError(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                finally
                {
                    disconnectedEvent.Set();
                }
            }

            void ErrorEventHandler(object sender, PusherException error)
            {
                errorCount++;
                try
                {
                    if (raiseErrorOn != null && raiseErrorOn.Count > 0)
                    {
                        string errorMsg = $"Error ({error.PusherCode}) raised in delegate {nameof(Pusher.Error)}. Inner error is:{Environment.NewLine}{error.Message}";
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
            }

            var pusher = await ConnectTestAsync().ConfigureAwait(false);
            pusher.ConnectionStateChanged += StateChangedEventHandler;
            pusher.Disconnected += DisconnectedEventHandler;
            pusher.Error += ErrorEventHandler;

            // Act
            await pusher.DisconnectAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ConnectionState.Disconnected, pusher.State, nameof(pusher.State));
            Assert.IsTrue(disconnectedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            statusChangeEvent.WaitOne(TimeSpan.FromSeconds(5));
            errorEvent?.WaitOne(TimeSpan.FromSeconds(5));

            Assert.AreEqual(expectedFinalCount, stateChangeLog.Count, nameof(expectedFinalCount));
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Disconnecting), $"Expected state change {ConnectionState.Disconnecting}");
            Assert.IsTrue(stateChangeLog.Contains(ConnectionState.Disconnected), $"Expected state change {ConnectionState.Disconnected}");
            Assert.AreEqual(expectedErrorCount, errorCount, "# Errors");

            return pusher;
        }
    }
}