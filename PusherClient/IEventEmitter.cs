using System;

namespace PusherClient
{
    public interface IEventEmitter<TData>: IEventBinder<TData>
    {
        TData ParseJson(string jsonData);

        void EmitEvent(string eventName, TData data);
    }
}