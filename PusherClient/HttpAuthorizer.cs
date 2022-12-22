using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PusherClient
{
    /// <summary>
    /// DEPRECATED: Use ChannelAuthorizer = new HttpChannelAuthorizer("http://example.com/auth") instead
    /// An implementation of the <see cref="IAuthorizer"/> using Http
    /// </summary>
    public class HttpAuthorizer: HttpChannelAuthorizer, IAuthorizer, IAuthorizerAsync
    {
        // /// <summary>
        // /// ctor
        // /// </summary>
        // /// <param name="authEndpoint">The End point to contact</param>
        public HttpAuthorizer(string authEndpoint) : base(authEndpoint)
        {
        }
    }
}
