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
            });
            _pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
            _pusher.Error += _pusher_Error;

            // Setup private channel
            _chatChannel = _pusher.SubscribeAsync("private-channel").Result;
            _chatChannel.Subscribed += ChatChannel_Subscribed;

            // Inline binding!
            _chatChannel.Bind("client-my-event", (dynamic data) =>
            {
                Console.WriteLine("[" + data.name + "] " + data.message);
            });

            // Setup presence channel
            _presenceChannel = (PresenceChannel)_pusher.SubscribeAsync("presence-channel").Result;
            _presenceChannel.Subscribed += PresenceChannel_Subscribed;
            _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
            _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;

            await _pusher.ConnectAsync();
        }

        static void _pusher_Error(object sender, PusherException error)
        {
            Console.WriteLine("Pusher Error: " + error);
        }

        static void _pusher_ConnectionStateChanged(object sender, ConnectionState state)
        {
            Console.WriteLine("Connection state: " + state);
        }

        // Presence Channel Events
        static void PresenceChannel_Subscribed(object sender)
        {
            ListMembers();
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

        // Chat Channel Events
        static void ChatChannel_Subscribed(object sender)
        {
            Console.WriteLine("Hi " + _name + "! Type 'quit' to exit, otherwise type anything to chat!");
        }
    }
}