namespace PusherClient
{
    public interface IEventEmitter<TData>: IEventBinder<TData>
    {
        void EmitEvent(string eventName, TData data);
    }
}