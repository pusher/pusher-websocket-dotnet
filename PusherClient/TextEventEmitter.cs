namespace PusherClient
{
    public class TextEventEmitter : EventEmitter<string>
    {
        public override string ParseJson(string jsonData)
        {
            return jsonData;
        }
    }
}
