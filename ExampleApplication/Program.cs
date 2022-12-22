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

            // Read input in loop
            string line;
            do
            {
                line = Console.ReadLine();

                if (line == "quit")
                {
                    break;
                }

                // _privateChannel.Trigger("client-my-event", new ChatMessage(message: line, name: _name));
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

            _pusher.Subscribed += (sender, channel) =>
            {
                string message = $"{Environment.NewLine}Subscribed To Channel{channel.Name}{Environment.NewLine}";
                Console.WriteLine(message);
            };

            _pusher.CountHandler += (sender, data) => 
            {
                Console.WriteLine(data);
            };

            // Setup private encrypted channel
            void GeneralListener(string eventName, PusherEvent eventData)
            {
                Console.WriteLine($"{Environment.NewLine} GeneralListner {eventName} {eventData.Data}");
                // ChatMessage data = JsonConvert.DeserializeObject<ChatMessage>(eventData.Data);
                // Console.WriteLine($"{Environment.NewLine}[{data.Name}] {data.Message}");
            }

            void DecryptionErrorHandler(object sender, PusherException error)
            {
                if (error is ChannelDecryptionException exception)
                {
                    string errorMsg = $"{Environment.NewLine}Decryption of message failed";
                    errorMsg += $" for ('{exception.ChannelName}',";
                    errorMsg += $" '{exception.EventName}', ";
                    errorMsg += $" '{exception.SocketID}')";
                    errorMsg += $" for reason:{Environment.NewLine}{error.Message}";
                    Console.WriteLine(errorMsg);
                }
            }

            _pusher.Error += DecryptionErrorHandler;
            _pusher.BindAll(GeneralListener);

            _pusher.Connected += (sender) =>
            {
                /*
                 * Setting up subscriptions here is handy if your App has the following setting - 
                 * "Enable authorized connections". See https://pusher.com/docs/channels/using_channels/authorized-connections.
                 * If your auth server is not running it will attempt to reconnect approximately every 30 seconds in an attempt to authenticate.
                 * Try running the ExampleApplication and entering your name without running the AuthHost. 
                 * You will see it try to authenticate every 30 seconds.
                 * Then run the AuthHost and see the ExampleApplication recover.
                 * Try this experiment again with multiple ExampleApplication instances running.
                 */
                try
                {
                    _publicChannel = _pusher.SubscribeAsync("my-channel").Result;
                }   
                catch (ChannelUnauthorizedException unauthorizedException)
                {
                    Console.WriteLine($"Authorization failed for {unauthorizedException.ChannelName}. {unauthorizedException.Message}");
                }
                _publicChannel.BindAll(ChannelEvent);

                try
                {
                    _privateChannel = _pusher.SubscribeAsync("private-my-channel").Result;
                }
                catch (ChannelUnauthorizedException unauthorizedException)
                {
                    Console.WriteLine($"Authorization failed for {unauthorizedException.ChannelName}. {unauthorizedException.Message}");
                }
                _privateChannel.BindAll(ChannelEvent);

                try
                {
                    _presenceChannel = (GenericPresenceChannel<ChatMember>)(_pusher.SubscribePresenceAsync<ChatMember>("presence-my-channel").Result);
                }
                catch (ChannelUnauthorizedException unauthorizedException)
                {
                    Console.WriteLine($"Authorization failed for {unauthorizedException.ChannelName}. {unauthorizedException.Message}");
                }
                _presenceChannel.BindAll(ChannelEvent);
                _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
                _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;

                try
                {
                    _privateEncryptedChannel = _pusher.SubscribeAsync("private-encrypted-my-channel").Result;
                }
                catch (ChannelUnauthorizedException unauthorizedException)
                {
                    Console.WriteLine($"Authorization failed for {unauthorizedException.ChannelName}. {unauthorizedException.Message}");
                }
                _privateEncryptedChannel.BindAll(ChannelEvent);


                _pusher.User.Bind(OnUserEvent);
                _pusher.User.Bind("blah", OnBlahUserEvent);

                
                _pusher.User.SigninAsync().Wait();
            };

            await _pusher.ConnectAsync().ConfigureAwait(false);
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

        static void OnBlahUserEvent(UserEvent eventData)
        {
            Console.WriteLine($"{Environment.NewLine} {eventData}");
            // TraceMessage(sender, $"{Environment.NewLine}{eventName} {eventData.Data}");
        }

        static void OnUserEvent(string eventName, UserEvent eventData)
        {
            Console.WriteLine($"{Environment.NewLine}{eventName} {eventData}");
            // TraceMessage(sender, $"{Environment.NewLine}{eventName} {eventData.Data}");
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