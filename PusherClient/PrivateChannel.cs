using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PusherClient
{
    public class PrivateChannel : Channel
    {
        public PrivateChannel(string channelName, Pusher pusher) : base(channelName, pusher) { }
    }
}
