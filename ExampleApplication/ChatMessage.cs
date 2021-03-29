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

        public string Message { get; set; }

    }
}