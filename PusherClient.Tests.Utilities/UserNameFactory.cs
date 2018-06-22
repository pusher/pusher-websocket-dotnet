using System;

namespace PusherClient.Tests.Utilities
{
    public static class UserNameFactory
    {
        public static string CreateUniqueUserName()
        {
            return $"testUser{DateTime.Now.Ticks}";
        }
    }
}
