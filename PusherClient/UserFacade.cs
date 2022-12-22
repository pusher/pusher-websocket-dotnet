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
        private IConnection _connection;
        private IPusher _pusher;

        // _signinLock protects the _isSignedIn flag and the _userChannel field.
        private SemaphoreSlim _signinLock = new SemaphoreSlim(1);
        private bool _isSignedIn = false;
        private Channel _userChannel;

        // _signinResultRecieved is initialized during the signin process in order to wait for the siginin success event.
        // When the signin success event is received, the semaphore is released.
        private SemaphoreSlim _signinResultRecieved;

        public UserFacade(IConnection connection, IPusher pusher)
        {
            _connection = connection;
            _pusher = pusher;

            // TODO Binding to pusher events doesn't work the following code.
            // This will probably require changes to IPusher to allow for binding to events.
            // _pusher.GetEventBinder(nameof(PusherEventEmitter)).Bind(Constants.PUSHER_SIGNIN_SUCCESS, (PusherEvent pusherEvent) =>
            // {
            //     _signinResultRecieved?.Release();
            // });

            // _pusher.GetEventBinder(nameof(PusherEventEmitter)).Bind(Constants.ERROR, (PusherEvent pusherEvent) =>
            // {
            //     // Check if this error is related to signin.
            //     // _signinResultRecieved?.Release();
                
            // });
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

        private async Task SinginProcess() {
            _signinResultRecieved = new SemaphoreSlim(0, 1);

            try {
                Console.WriteLine($"{_connection} Cluster {_pusher.PusherOptions.Cluster}, {_pusher.PusherOptions.UserAuthenticator}");


                // Wait for the connection to be connected.
                await _connection.ConnectAsync().ConfigureAwait(false);

                string jsonAuth;
                if (_pusher.PusherOptions.UserAuthenticator is IUserAuthenticatorAsync asyncAuthenticator)
                {
                    if (!asyncAuthenticator.Timeout.HasValue)
                    {
                        // Use a timeout interval that is less than the outer subscription timeout.
                        asyncAuthenticator.Timeout = _pusher.PusherOptions.InnerClientTimeout;
                    }

                    string socketId = _connection.SocketId;

                    jsonAuth = await asyncAuthenticator.AuthenticateAsync(socketId).ConfigureAwait(false);
                }
                else
                {
                    jsonAuth = _pusher.PusherOptions.UserAuthenticator.Authenticate(_connection.SocketId);
                }

                // TODO parse the jsonAuth and get the user id
                string userId = "TODO123";

                // Send signin event on the connection
                string message = CreateSigninMessage(jsonAuth);
                await _connection.SendAsync(message).ConfigureAwait(false);

                TimeSpan timeoutPeriod = _pusher.PusherOptions.InnerClientTimeout;
                if (!await _signinResultRecieved.WaitAsync(timeoutPeriod).ConfigureAwait(false))
                {
                    throw new OperationTimeoutException(timeoutPeriod, $"{Constants.PUSHER_SIGNIN_SUCCESS}");
                }

                _isSignedIn = true;

                // Subscribe to the user channel
                _userChannel = await _pusher.SubscribeAsync(Constants.USER_CHANNEL_PREFIX + userId);

                // TODO Binding to a channel event doesn't work in this way:
                // _userChannel.Bind((string eventName, PusherEvent pusherEvent) =>
                // {
                //     // TODO The interface of the user events is not determined yet.
                //     EmitEvent(eventName, new UserEvent(null, pusherEvent.Data));
                // });
            }
            finally
            {
                // Call any error handler to report the error

                _signinResultRecieved.Release();
                _signinResultRecieved = null;
            }
        }

        private string CreateSigninMessage(string jsonAuth)
        {
            string auth = null;
            string userData = null;
            JObject jObject = JObject.Parse(jsonAuth);
            JToken jToken = jObject.SelectToken("auth");
            if (jToken != null)
            {
                if (jToken.Type == JTokenType.String)
                {
                    auth = jToken.Value<string>();
                }
            }

            jToken = jObject.SelectToken("user_data");
            if (jToken != null && jToken.Type == JTokenType.String)
            {
                userData = jToken.Value<string>();
            }


            PusherSigninEventData data = new PusherSigninEventData(auth, userData);
            return DefaultSerializer.Default.Serialize(new PusherSigninEvent(data));
        }


        // public WatchlistFacade Watchlist();

    }
}