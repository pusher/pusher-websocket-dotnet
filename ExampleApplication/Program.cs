using System;
using System.Collections.Generic;
using System.Linq;
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
        private static PresenceChannel _presenceChannel;
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
            var names = new List<string>();

            foreach (var mem in _presenceChannel.Members)
            {
                names.Add((string)mem.Value.name.Value);
            }

            Console.WriteLine("[MEMBERS] " + names.Aggregate((i, j) => i + ", " + j));
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
            _pusher.Subscribed += Channel_Subscribed;

            // Setup private channel
            _chatChannel = await _pusher.SubscribeAsync("private-channel").ConfigureAwait(false);

            // Inline binding!
            _chatChannel.Bind("client-my-event", (dynamic data) =>
            {
                Console.WriteLine("[" + data.name + "] " + data.message);
            });

            // Setup presence channel
            _presenceChannel = (PresenceChannel)(await _pusher.SubscribeAsync("presence-channel").ConfigureAwait(false));
            _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
            _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;

            await _pusher.ConnectAsync();
        }

        static void PusherError(object sender, PusherException error)
        {
            Console.WriteLine("Pusher Error: " + error);
        }

        static void PusherConnectionStateChanged(object sender, ConnectionState state)
        {
            Console.WriteLine("Connection state: " + state);
        }

        static void PresenceChannel_MemberRemoved(object sender)
        {
            ListMembers();
        }

        static void PresenceChannel_MemberAdded(object sender, KeyValuePair<string, dynamic> member)
        {
            Console.WriteLine((string)member.Value.name.Value + " has joined");
            ListMembers();
        }

        // Channel Events
        static void Channel_Subscribed(object sender, string channelName)
        {
            Pusher pusher = sender as Pusher;
            Channel channel = pusher.GetChannel(channelName);
            Console.WriteLine();
            Console.WriteLine($"Subscribed to {channelName}.");
            if (channel.ChannelType == ChannelTypes.Private)
            {
                Console.WriteLine($"Hi {_name}! Type 'quit' to exit, otherwise type anything to chat!");
            }
            else if (channel.ChannelType == ChannelTypes.Presence)
            {
                ListMembers();
            }

            Console.WriteLine();
        }
    }
}