using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PusherClient;

namespace ExampleApplication
{
    class Program
    {
        private static Pusher _pusher;
        private static Channel _chatChannel;
        private static PresenceChannel _presenceChannel;
        private static string _name;
            
        static void Main(string[] args)
        {
            // Get the user's name
            Console.WriteLine("What is your name?");
            _name = Console.ReadLine();

            InitPusher();

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

            _pusher.Disconnect();
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
        private static void InitPusher()
        {
            _pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new HttpAuthorizer("http://localhost:8888/auth/" + HttpUtility.UrlEncode(_name))
            });
            _pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
            _pusher.Error += _pusher_Error;

            // Setup private channel
            _chatChannel = _pusher.Subscribe("private-channel");
            _chatChannel.Subscribed += ChatChannel_Subscribed;

            // Inline binding!
            _chatChannel.Bind("client-my-event", (dynamic data) =>
            {
                Console.WriteLine("[" + data.name + "] " + data.message);
            });

            // Setup presence channel
            _presenceChannel = (PresenceChannel)_pusher.Subscribe("presence-channel");
            _presenceChannel.Subscribed += PresenceChannel_Subscribed;
            _presenceChannel.MemberAdded += PresenceChannel_MemberAdded;
            _presenceChannel.MemberRemoved += PresenceChannel_MemberRemoved;

            _pusher.Connect();
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