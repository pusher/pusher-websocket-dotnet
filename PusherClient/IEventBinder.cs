using System;

namespace PusherClient
{
    public interface IEventBinder<TData>
    {
        void Bind(string eventName, Action<TData> listener);
        void Bind(Action<string, TData> listener);
        void Unbind(string eventName);
        void Unbind(string eventName, Action<TData> listener);
        void UnbindAll();
    }
}