using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PusherClient
{
    public interface IAuthorizer
    {
        Task<string> Authorize(string channelName, string socketId);
    }
}
