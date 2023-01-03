using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PusherClient
{
    internal class UserFacade : EventEmitter<UserEvent>, IUserFacade
    {
        public IWatchlistFacade Watchlist { 
            get {
                return _watchlistFacade;
            }
        }
        private WatchlistFacade _watchlistFacade = new WatchlistFacade();

        Func<IConnection> _getConnection;
        private IPusher _pusher;
        private Channel _userChannel;
        private User user = null;

        /// protected by _stateLock
        private SemaphoreSlim _stateLock = new SemaphoreSlim(1);
        private bool _isSinginRequested = false;
        private bool _isConnected = false;
        private TaskCompletionSource<object> _signinDonePromise = null;
        /// protected by _stateLock

        private async Task _CriticalSection(Action action) {
            try
            {
                TimeSpan timeoutPeriod = _pusher.PusherOptions.ClientTimeout;
                if (!await _stateLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.PUSHER_SIGNIN}");
                }

                action();
            }
            finally
            {
                _stateLock.Release();
            }
        }


        public UserFacade(Func<IConnection> getConnection, IPusher pusher)
        {
            _getConnection = getConnection;
            _pusher = pusher;
        }

        // This method records that Signin is requested on this connection.
        // And starts it if it wasn't started before.
        public async void Signin() {
            bool shouldReturn = false;
            await _CriticalSection(() => {
                if (_isSinginRequested) {
                    shouldReturn = true;
                    return;
                }
                _isSinginRequested = true;
            });
            if (shouldReturn) {
                return;
            }

            _Signin();
        }

        public async Task SigninDoneAsync() {
            TaskCompletionSource<object> promise = null;
            await _CriticalSection(() => {
                promise = _signinDonePromise;
            }).ConfigureAwait(false);
            await promise.Task;
        }


        internal async void OnConnectionStateChanged(object sender, ConnectionState state) {
            bool previousIsConnected = false;
            await _CriticalSection(() => {
                previousIsConnected = _isConnected;

                if (state == ConnectionState.Connected)
                {
                    _isConnected = true;
                }

                if (state != ConnectionState.Connected)
                {
                    _isConnected = false;
                }
            }).ConfigureAwait(false);

            if (!previousIsConnected && _isConnected) {
                await _Signin();
            }

            if (previousIsConnected && !_isConnected) {
                await _Cleanup();
                await _NewSigninPromiseIfNeeded();
            }
        }

        private async Task _Cleanup(Exception error = null) {
            await _CriticalSection(() => {
                if(_userChannel != null) {
                    _userChannel.UnbindAll();
                    _userChannel.Unsubscribe();
                    _userChannel = null;
                }
                // TODO _watchlistFacade.Cleanup();

                if(_isSinginRequested) {
                    if(error == null) {
                        _signinDonePromise.TrySetResult(null);
                    } else {
                        _signinDonePromise.TrySetException(error);
                    }
                }
            }).ConfigureAwait(false);
        }

        private async Task _Signin() {
            bool shouldReturn = false;
            await _CriticalSection(() => {
                if (!_isSinginRequested) {
                    shouldReturn = true;
                }
            }).ConfigureAwait(false);
            if (shouldReturn) {
                return;
            }

            await this._NewSigninPromiseIfNeeded();

            await _CriticalSection(() => {
                if(!_isConnected) {
                    shouldReturn = true;
                }
            });
            if (shouldReturn) {
                return;
            }

            // The current code doesn't prevent SinginProcess from being called twice in parallel.
            SinginProcess();
        }


        private async Task _NewSigninPromiseIfNeeded() {
            await _CriticalSection(() => {
                if (!_isSinginRequested) {
                    return;
                }

                // If there is a promise and it is not resolved, return without creating a new one.
                if (_signinDonePromise != null && !_signinDonePromise.Task.IsCompleted) {
                    return;
                }

                // Either there is no promise or the promise is already completed. We need to create a new one.
                _signinDonePromise = new TaskCompletionSource<object>();
            }).ConfigureAwait(false);
        }


        internal void OnPusherEvent(string eventName, PusherEvent pusherEvent) {
            if (eventName == Constants.PUSHER_SIGNIN_SUCCESS) {
                _OnSigninSuccess(pusherEvent);
            }
            _watchlistFacade.OnPusherEvent(eventName, pusherEvent);
        }

        private void _OnSigninSuccess(PusherEvent pusherEvent) {
            // Try to parse event
            try {
                String data = pusherEvent.Data;
                String userData = null;
                JObject jObject = JObject.Parse(data);
                JToken jToken = jObject.SelectToken("user_data");
                if (jToken != null)
                {
                    if (jToken.Type == JTokenType.String)
                    {
                        userData = jToken.Value<string>();
                    }
                }

                if (userData == null)
                {
                    throw new Exception("user_data is null");
                }

                user = ParseUser(userData);
            } catch (Exception error) {
                Console.WriteLine($"{Environment.NewLine} error parsing user {error}");
                this._Cleanup(error);
                return;
            }
                    
            // TODO Check if user_data contains an id, string, and not empty.
            // Otherwise, report error.

            _signinDonePromise.TrySetResult(null);
            _SubscribeToUserChannel();
        }

        private async void _SubscribeToUserChannel() {
            // TODO check if something else is needed to subscribe to user channel
            _userChannel = await _pusher.SubscribeAsync(Constants.USER_CHANNEL_PREFIX + user.id);
            _userChannel.BindAll(OnUserChannelEvent);
        }

        private async Task SinginProcess() {
            try {
                IConnection connection = _getConnection();
                string socketId = connection.SocketId;
                string jsonAuth;
                if (_pusher.PusherOptions.UserAuthenticator is IUserAuthenticatorAsync asyncAuthenticator)
                {
                    if (!asyncAuthenticator.Timeout.HasValue)
                    {
                        // Use a timeout interval that is less than the outer subscription timeout.
                        asyncAuthenticator.Timeout = _pusher.PusherOptions.InnerClientTimeout;
                    }

                    jsonAuth = await asyncAuthenticator.AuthenticateAsync(socketId).ConfigureAwait(false);
                }
                else
                {
                    jsonAuth = _pusher.PusherOptions.UserAuthenticator.Authenticate(connection.SocketId);
                }

                UserAuthResponse authResponse = ParseAuthMessage(jsonAuth);

                // Send signin event on the connection
                string message = CreateSigninMessage(authResponse);
                await connection.SendAsync(message).ConfigureAwait(false);
            } catch (Exception error) {
                this._Cleanup(error);
                return;
            }
        }

        struct UserAuthResponse {
            internal string auth;
            internal string userData;
        };
        
        class User {
            internal string id;
        };

        private UserAuthResponse ParseAuthMessage(string jsonAuth)
        {
            UserAuthResponse authResponse = new UserAuthResponse();
            JObject jObject = JObject.Parse(jsonAuth);
            JToken jToken = jObject.SelectToken("auth");
            if (jToken != null)
            {
                if (jToken.Type == JTokenType.String)
                {
                    authResponse.auth = jToken.Value<string>();
                }
            }

            jToken = jObject.SelectToken("user_data");
            if (jToken != null && jToken.Type == JTokenType.String)
            {
                authResponse.userData = jToken.Value<string>();
            }

            return authResponse;
        }

        private User ParseUser(string jsonAuth)
        {
            User user = new User();
            JObject jObject = JObject.Parse(jsonAuth);
            JToken jToken = jObject.SelectToken("id");
            if (jToken != null)
            {
                if (jToken.Type == JTokenType.String)
                {
                    user.id = jToken.Value<string>();
                }
            }

            return user;
        }


        private string CreateSigninMessage(UserAuthResponse authResponse)
        {
            PusherSigninEventData data = new PusherSigninEventData(authResponse.auth, authResponse.userData);
            return DefaultSerializer.Default.Serialize(new PusherSigninEvent(data));
        }

        private void OnUserChannelEvent(string eventName, PusherEvent pusherEvent)
        {
            EmitEvent(eventName, new UserEvent(pusherEvent.Data));
        }


    }
}