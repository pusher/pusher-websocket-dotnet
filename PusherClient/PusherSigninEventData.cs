using Newtonsoft.Json;

namespace PusherClient
{
    internal class PusherSigninEventData
    {
        public PusherSigninEventData(string auth, string userData)
        {
            this.Auth = auth;
            this.UserData = userData;
        }

        [JsonProperty(PropertyName = "auth")]
        public string Auth { get; }

        [JsonProperty(PropertyName = "user_data")]
        public string UserData { get; }
    }
}
