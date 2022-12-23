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
        Func<IConnection> _getConnection;
        private IPusher _pusher;

        // _signinLock protects the _isSignedIn flag and the _userChannel field.
        private SemaphoreSlim _signinLock = new SemaphoreSlim(1);
        private bool _isSignedIn = false;
        private Channel _userChannel;

        // _signinResultRecieved is initialized during the signin process in order to wait for the siginin success event.
        // When the signin success event is received, the semaphore is released.
        private SemaphoreSlim _signinResultRecieved;


        private SemaphoreSlim _connectedLock = new SemaphoreSlim(0, 1);


        public IWatchlistFacade Watchlist { 
            get {
                return _watchlistFacade;
            }
        }

        private WatchlistFacade _watchlistFacade = new WatchlistFacade();

        public UserFacade(Func<IConnection> getConnection, IPusher pusher)
        {
            _getConnection = getConnection;
            _pusher = pusher;
        }

        public async Task SigninAsync() {
            try
            {
                TimeSpan timeoutPeriod = _pusher.PusherOptions.ClientTimeout;
                if (!await _signinLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.PUSHER_SIGNIN}");
                }

                if (!_isSignedIn)
                {
                    await SinginProcess().ConfigureAwait(false);
                }
            }
            catch (Exception error)
            {
                // TODO handle error
                throw;
            }
            finally
            {
                _signinLock.Release();
            }
        }

        internal void OnConnectionStateChanged(object sender, ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                _connectedLock.Release();
            }
        }

        internal void OnPusherEvent(string eventName, PusherEvent pusherEvent) {
            if (eventName == Constants.PUSHER_SIGNIN_SUCCESS) {
                _signinResultRecieved?.Release();
            }
            _watchlistFacade.OnPusherEvent(eventName, pusherEvent);
        }

        private async Task SinginProcess() {
            _signinResultRecieved = new SemaphoreSlim(0, 1);

            try {
                TimeSpan timeoutPeriod = _pusher.PusherOptions.InnerClientTimeout;

                // Wait for the connection to be connected.
                if (!await _connectedLock.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.PUSHER_SIGNIN_SUCCESS}");
                }
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
                User user = ParseUser(authResponse.userData);

                // Send signin event on the connection
                string message = CreateSigninMessage(authResponse);
                await connection.SendAsync(message).ConfigureAwait(false);

                if (!await _signinResultRecieved.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.PUSHER_SIGNIN_SUCCESS}");
                }

                _isSignedIn = true;

                // Subscribe to the user channel
                _userChannel = await _pusher.SubscribeAsync(Constants.USER_CHANNEL_PREFIX + user.id);
                _userChannel.BindAll(OnUserChannelEvent);
            }
            finally
            {
                // TODO Call any error handler to report the error

                _signinResultRecieved.Release();
                _signinResultRecieved = null;
            }
        }

        struct UserAuthResponse {
            internal string auth;
            internal string userData;
        };
        
        struct User {
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