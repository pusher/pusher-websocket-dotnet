using System;
using System.Reflection;

namespace PusherClient
{
    [Obsolete("This interface has been deprecated. Please use IChannelAuthorizer")]
    public interface IAuthorizer : IChannelAuthorizer {}
}