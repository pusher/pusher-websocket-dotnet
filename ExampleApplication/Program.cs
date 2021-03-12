using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PusherClient;
using PusherClient.Tests.Utilities;

namespace ExampleApplication
{
    class Program
    {
        private static Pusher _pusher;
        private static Channel _chatChannel;
        private static GenericPresenceChannel<dynamic> _presenceChannel;
        private static string _name;
            
        static void Main()
        {
            // Get the user's name
            Console.WriteLine("What is your name?");
            _name = Console.ReadLine();

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

                _chatChannel.Trigger("client-my-event", new {message = line, name = _name});
            } while (line != null);

            var disconnectResult = Task.Run(() => _pusher.DisconnectAsync());
            Task.WaitAll(disconnectResult);
        }

        static void ListMembers()
        {
            StringBuilder builder = new StringBuilder($"{Environment.NewLine}[MEMBERS]{Environment.NewLine}");
            int count = 1;
            foreach (var mem in _presenceChannel.GetMembers())
            {
                builder.AppendLine($"{count}: {(string)mem.Value.name.Value}");
                count++;
            }

            Console.WriteLine(builder.ToString());
        }

        // Pusher Initiation / Connection
        private static async Task InitPusher()
        {
            _pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new HttpAuthorizer("http://localhost:8888/auth/" + HttpUtility.UrlEncode(_name)),
                Cluster = Config.Cluster,
                Encrypted = Config.Encrypted,
                IsTracingEnabled = true,
            });
            _pusher.ConnectionStateChanged += PusherConnectionStateChanged;
            _pusher.Error += PusherError;

            // Setup private channel
            string privateChannelName = "private-channel";
            _pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == privateChannelName)
                {
                    string message = $"{Environment.NewLine}Hi {_name}! Type 'quit' to exit, otherwise type anything to chat!{Environment.NewLine}";
                    Console.WriteLine(message);
                }
            };
            _chatChannel = await _pusher.SubscribeAsync(privateChannelName).ConfigureAwait(false);

            // Inline binding!
            _chatChannel.Bind("client-my-event", (dynamic data) =>
            {
                Console.WriteLine("[" + data.name + "] " + data.message);
            });

            // Setup presence channel
            string presenceChannelName = "presence-channel";
            _pusher.Subscribed += (sender, channel) =>
            {
                if (channel.Name == presenceChannelName)
                {
                    ListMembers();
                }
            };
            _presenceChannel = (GenericPresenceChannel<dynamic>)(await _pusher.SubscribePresenceAsync<dynamic>(presenceChannelName).ConfigureAwait(false));
            _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
            _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;

            await _pusher.ConnectAsync().ConfigureAwait(false);
        }

        static void PusherError(object sender, PusherException error)
        {
            Console.WriteLine("Pusher Error: " + error);
        }

        static void PusherConnectionStateChanged(object sender, ConnectionState state)
        {
            Console.WriteLine("Connection state: " + state);
        }

        static void PresenceChannel_MemberRemoved(object sender, KeyValuePair<string, dynamic> member)
        {
            Console.WriteLine($"Member {member.Value.name.Value} has left");
            ListMembers();
        }

        static void PresenceChannel_MemberAdded(object sender, KeyValuePair<string, dynamic> member)
        {
            Console.WriteLine($"Member {member.Value.name.Value} has joined");
            ListMembers();
        }
    }
}