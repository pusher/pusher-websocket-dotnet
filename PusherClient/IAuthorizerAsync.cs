using System;
using System.Reflection;


namespace PusherClient
{
    [Obsolete("This interface has been deprecated. Please use IChannelAuthorizerAsync")]
    public interface IAuthorizerAsync : IChannelAuthorizerAsync {}
}