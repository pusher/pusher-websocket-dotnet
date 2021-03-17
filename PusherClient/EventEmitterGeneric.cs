using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PusherClient
{
    public class EventEmitter<TData> : IEventBinder<TData>, IEmitter<TData>
    {
        private readonly ConcurrentDictionary<string, List<Action<TData>>> _listeners = new ConcurrentDictionary<string, List<Action<TData>>>();
        private readonly ConcurrentStack<Action<string, TData>> _generalListeners = new ConcurrentStack<Action<string, TData>>();

        internal Action<PusherException> ErrorHandler;

        /// <summary>
        /// Binds to a given event with <paramref name="eventName"/>.
        /// </summary>
        /// <param name="eventName">The event name to listen for.</param>
        /// <param name="listener">The action to perform when the event occurs.</param>
        public void Bind(string eventName, Action<TData> listener)
        {
            Guard.EventName(eventName);

            if (listener != null)
            {
                _listeners.TryAdd(eventName, new List<Action<TData>>());
                _listeners[eventName].Add(listener);
            }
        }

        /// <summary>
        /// Binds a listener that listens to all events.
        /// </summary>
        /// <param name="listener">The action to perform when any event occurs.</param>
        public void Bind(Action<string, TData> listener)
        {
            if (listener != null)
            {
                _generalListeners.Push(listener);
            }
        }

        /// <summary>
        /// Remove the action for the event name.
        /// </summary>
        /// <param name="eventName">The name of the event to unbind.</param>
        /// <param name="listener">The action to remove.</param>
        public void Unbind(string eventName, Action<TData> listener)
        {
            Guard.EventName(eventName);

            if (listener != null)
            {
                if (_listeners.ContainsKey(eventName))
                {
                    _listeners[eventName].Remove(listener);
                }
            }
        }

        /// <summary>
        /// Removes the binding for the given event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        public void Unbind(string eventName)
        {
            Guard.EventName(eventName);

            if (_listeners.TryRemove(eventName, out List<Action<TData>> items))
            {
                items.Clear();
            }
        }

        /// <summary>
        /// Removes all bindings.
        /// </summary>
        public void UnbindAll()
        {
            _listeners.Clear();
            _generalListeners.Clear();
        }

        void IEmitter<TData>.EmitEvent(string eventName, TData data)
        {
            foreach (var a in _generalListeners)
            {
                try
                {
                    a(eventName, data);
                }
                catch (Exception e)
                {
                    HandleException(e, eventName, data);
                }
            }

            if (_listeners.TryGetValue(eventName, out var items))
            {
                foreach (var a in items)
                {
                    try
                    {
                        a(data);
                    }
                    catch (Exception e)
                    {
                        HandleException(e, eventName, data);
                    }
                }
            }
        }

        private void HandleException(Exception e, string eventName, TData data)
        {
            if (ErrorHandler != null)
            {
                PusherException errorToHandle = e as PusherException;
                if (errorToHandle == null)
                {
                    errorToHandle = new EventEmitterActionException<TData>(ErrorCodes.EventEmitterActionError, eventName, data, e);
                }

                try
                {
                    ErrorHandler.Invoke(errorToHandle);
                }
                catch (Exception errorHandlerError)
                {
                    // Not much we can do if the error handler also fails
                    System.Diagnostics.Trace.TraceError(errorHandlerError.ToString());
                }
            }
        }
    }
}
