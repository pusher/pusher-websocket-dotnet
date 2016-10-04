namespace PusherClient
{
    public class PusherOptions
    {
        public bool Encrypted = false;
        public IAuthorizer Authorizer = null;
        public string Host = "ws.pusher.com";
    }
}
