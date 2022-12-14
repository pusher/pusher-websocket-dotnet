using System;
using System.Collections.Generic;
using System.Reflection;


namespace PusherClient
{
    [Obsolete("This class has been deprecated. Please use HttpChannelAuthorizer")]
    public class HttpAuthorizer: HttpChannelAuthorizer, IAuthorizer, IAuthorizerAsync
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="authEndpoint">The End point to contact</param>
        public HttpAuthorizer(string authEndpoint) : base(authEndpoint){}
    }
}
