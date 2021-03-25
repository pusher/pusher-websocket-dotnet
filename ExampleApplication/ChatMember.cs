using Newtonsoft.Json;

namespace ExampleApplication
{
    internal class ChatMember
    {
        public ChatMember()
        {
        }

        public ChatMember(string name)
        {
            this.Name = name;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
