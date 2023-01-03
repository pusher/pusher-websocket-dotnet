using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using PusherClient;
using PusherClient.Tests.Utilities;

namespace ExampleApplication
{
    class Program
    {
        private static Pusher _pusher;
        private static Channel _publicChannel;
        private static Channel _privateChannel;
        private static Channel _privateEncryptedChannel;
        
        private static GenericPresenceChannel<ChatMember> _presenceChannel;

        static void Main()
        {
            var connectResult = Task.Run(() => InitPusher());
            Task.WaitAll(connectResult);

            // Read input in loop to keep the program running
            string line;
            do
            {
                line = Console.ReadLine();

                if (line == "quit")
                {
                    break;
                }
            } while (line != null);

            var disconnectResult = Task.Run(() => _pusher.DisconnectAsync());
            Task.WaitAll(disconnectResult);
        }

        private static async Task InitPusher()
        {
            _pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                ChannelAuthorizer = new HttpChannelAuthorizer("http://127.0.0.1:9000/pusher/auth"),
                UserAuthenticator = new HttpUserAuthenticator("http://127.0.0.1:9000/pusher/auth-user"),
                Cluster = Config.Cluster,
                Encrypted = Config.Encrypted,
                TraceLogger = new TraceLogger(),
            });
            _pusher.ConnectionStateChanged += PusherConnectionStateChanged;
            _pusher.Error += PusherError;
            _pusher.Subscribed += Subscribed;
            _pusher.CountHandler += CountHandler;
            _pusher.Connected += Connected;
            _pusher.BindAll(GeneralListener);

            _pusher.User.Signin();
            _pusher.User.Bind(OnUserEvent);
            _pusher.User.Bind("test_event", OnBlahUserEvent);
            _pusher.User.Watchlist.Bind("online", OnWatchlistOnlineEvent);
            _pusher.User.Watchlist.Bind("offline", OnWatchlistOfflineEvent);
            _pusher.User.Watchlist.Bind(OnWatchlistEvent);

            try
            {
                TimeSpan timeoutPeriod = TimeSpan.FromSeconds(10);
                _publicChannel = await _pusher.SubscribeAsync("my-channel").WaitAsync(timeoutPeriod);
                _publicChannel.BindAll(ChannelEvent);

                _privateChannel = await _pusher.SubscribeAsync("private-my-channel").WaitAsync(timeoutPeriod);
                _privateChannel.BindAll(ChannelEvent);

                _presenceChannel = await _pusher.SubscribePresenceAsync<ChatMember>("presence-my-channel").WaitAsync(timeoutPeriod);
                _presenceChannel.BindAll(ChannelEvent);
                _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
                _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;
                
                _privateEncryptedChannel = await _pusher.SubscribeAsync("private-encrypted-my-channel").WaitAsync(timeoutPeriod);
                _privateEncryptedChannel.BindAll(ChannelEvent);

            }
            catch (ChannelUnauthorizedException unauthorizedException)
            {
                Console.WriteLine($"Authorization failed for {unauthorizedException.ChannelName}. {unauthorizedException.Message}");
            }

            Console.WriteLine("All SubscribeAsync already called");

            await _pusher.ConnectAsync().ConfigureAwait(false);

            await _pusher.User.SigninDoneAsync();
            
            
            await Task.Delay(10000).ConfigureAwait(false);
            Console.WriteLine($"Test Disconnect & Connect");
            await _pusher.DisconnectAsync().ConfigureAwait(false);
            await Task.Delay(2000).ConfigureAwait(false);
            await _pusher.ConnectAsync().ConfigureAwait(false);
            await _pusher.User.SigninDoneAsync();
        }

        static void ListMembers()
        {
            if (_presenceChannel != null)
            {
                StringBuilder builder = new StringBuilder($"{Environment.NewLine}[MEMBERS]{Environment.NewLine}");
                int count = 1;
                var sorted = _presenceChannel.GetMembers().Select(m => m).OrderBy(m => m.Value.Name);
                foreach (var member in sorted)
                {
                    builder.AppendLine($"{count}: {member.Value.Name}");
                    count++;
                }

                Console.WriteLine(builder.ToString());
            }
        }

        // Pusher Initiation / Connection

        // Setup private encrypted channel
        static void GeneralListener(string eventName, PusherEvent eventData)
        {
            Console.WriteLine($"{Environment.NewLine} GeneralListner {eventName} {eventData.Data}");
        }

        static void Connected(object sender)
        {
            Console.WriteLine($"Connected");
        }
        
        static void Subscribed(object sender, Channel channel)
        {
            Console.WriteLine($"Subscribed To Channel {channel.Name}");
        }
        
        static void CountHandler(object sender, string data)
        {
            Console.WriteLine($"CountHandler {data}");
        }

        static void OnBlahUserEvent(UserEvent userEvent)
        {
            Console.WriteLine($"{Environment.NewLine} OnBlahUserEvent {userEvent}");
        }

        static void OnUserEvent(string eventName, UserEvent userEvent)
        {
            Console.WriteLine($"{Environment.NewLine} UserEvent {eventName} {userEvent}");
        }

        static void OnWatchlistEvent(string eventName, WatchlistEvent watchlistEvent)
        {
            Console.WriteLine($"{Environment.NewLine} OnWatchlistEvent {eventName} {watchlistEvent.Name}");
            foreach (var id in watchlistEvent.UserIDs)
            {
                Console.WriteLine($"{Environment.NewLine} OnWatchlistEvent {eventName} {watchlistEvent.Name} {id}");
            }
        }
        static void OnWatchlistOnlineEvent(WatchlistEvent watchlistEvent)
        {
            Console.WriteLine($"{Environment.NewLine} OnWatchlistOnlineEvent {watchlistEvent}");
        }
        static void OnWatchlistOfflineEvent(WatchlistEvent watchlistEvent)
        {
            Console.WriteLine($"{Environment.NewLine} OnWatchlistOfflineEvent {watchlistEvent}");
        }


        static void ChannelEvent(string eventName, PusherEvent eventData)
        {
            Console.WriteLine($"{Environment.NewLine}{eventName} {eventData.Data}");
            // TraceMessage(sender, $"{Environment.NewLine}{eventName} {eventData.Data}");
        }

        static void PusherError(object sender, PusherException error)
        {
            TraceMessage(sender, $"{Environment.NewLine}Pusher Error: {error.Message}{Environment.NewLine}{error}");
        }

        static void PusherConnectionStateChanged(object sender, ConnectionState state)
        {
            TraceMessage(sender, $"Connection state: {state}");
        }

        static void PresenceChannel_MemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
        {
            Console.WriteLine($"Member {member.Value.Name} has left");
            ListMembers();
        }

        static void PresenceChannel_MemberAdded(object sender, KeyValuePair<string, ChatMember> member)
        {
            Console.WriteLine($"Member {member.Value.Name} has joined");
            ListMembers();
        }

        static void TraceMessage(object sender, string message)
        {
            Pusher client = sender as Pusher;
            Console.WriteLine($"{DateTime.Now:o} - {client.SocketID} - {message}");
        }
    }
}