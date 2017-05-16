namespace PusherClient
{
    public class PusherOptions
    {
        public bool Encrypted = false;
        public IAuthorizer Authorizer = null;
        public string Cluster = "mt1";

        internal string Host => $"ws-{this.Cluster}.pusher.com";
    }
}