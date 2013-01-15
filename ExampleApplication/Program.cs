using PusherClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ExampleApplication
{

    class Program
    {
        static Pusher _pusher = null;
        static Channel _chatChannel = null;
        static PresenceChannel _presenceChannel = null;
        static string _name;
            
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
                    break;
                else
                    _chatChannel.Trigger("client-my-event", new { message = line, name = _name });            

            } while (line != null);

            _pusher.Disconnect();

        }

        #region Pusher Initiation / Connection

        private static void InitPusher()
        {
            _pusher = new Pusher("527784eba833ff169524");
            _pusher.ChannelAuthEndpoint = "http://localhost:8888/auth/" + HttpUtility.UrlEncode(_name);
            _pusher.Connected += pusher_Connected;
            _pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
            _pusher.Connect();
        }

        static void _pusher_ConnectionStateChanged(object sender, ConnectionState state)
        {
            Console.WriteLine("Connection state: " + state.ToString());
        }

        static void pusher_Connected(object sender)
        {
            // Setup private channel
            _chatChannel = _pusher.Subscribe("private-channel");
            _chatChannel.Subscribed += _chatChannel_Subscribed;

            // Setup presence channel
            _presenceChannel = (PresenceChannel)_pusher.Subscribe("presence-channel");
            _presenceChannel.Subscribed += _presenceChannel_Subscribed;
            _presenceChannel.MemberAdded += _presenceChannel_MemberAdded;
            _presenceChannel.MemberRemoved += _presenceChannel_MemberRemoved;
        }

        #endregion

        #region Presence Channel Events

        static void _presenceChannel_Subscribed(object sender)
        {
            ListMembers();
        }

        static void _presenceChannel_MemberRemoved(object sender)
        {
            ListMembers();
        }

        static void _presenceChannel_MemberAdded(object sender)
        {
            ListMembers();
        }

        #endregion

        #region Chat Channel Events

        static void _chatChannel_Subscribed(object sender)
        {
            Console.WriteLine("Hi " + _name + "! Type 'quit' to exit, otherwise type anything to chat!");

            _chatChannel.Bind("client-my-event", (dynamic data) =>
            {
                Console.WriteLine("[" + data.name + "] " + data.message);
            });
        }

        #endregion

        static void ListMembers()
        {
            List<string> names = new List<string>();

            foreach (var mem in _presenceChannel.Members)
            {
                names.Add((string)mem.Value.name);
            }

            Console.WriteLine("[MEMBERS] " + names.Aggregate((i,j) => i + ", " + j ));
        }
        
    }

}
