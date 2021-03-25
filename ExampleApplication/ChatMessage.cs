using Newtonsoft.Json;

namespace ExampleApplication
{
    internal class ChatMessage : ChatMember
    {
        public ChatMessage()
            : base()
        {
        }

        public ChatMessage(string message, string name)
            : base(name)
        {
            this.Message = message;
        }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

    }
}