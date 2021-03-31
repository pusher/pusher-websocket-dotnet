using System;

namespace PusherClient
{
    public interface IEventBinder
    {
        Action<PusherException> ErrorHandler { get; set; }

        bool HasListeners { get; }

        void Unbind(string eventName);

        void UnbindAll();
    }
}