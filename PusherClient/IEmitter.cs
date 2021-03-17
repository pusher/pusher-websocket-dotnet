using System;

namespace PusherClient
{
    internal interface IEmitter<TData>
    {
        void EmitEvent(string eventName, TData data);
    }
}