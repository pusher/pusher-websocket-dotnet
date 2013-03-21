using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PusherClient
{
    public interface IAuthorizer
    {
        string Authorize(string channelName, string socketId);
    }
}
