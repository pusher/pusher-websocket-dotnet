using System;
using System.Collections.Generic;

namespace PusherClient
{
    public class EventEmitter<TData> : IEventEmitter<TData>
    {
        private readonly object _sync = new object();
        private readonly IDictionary<string, List<Action<TData>>> _listeners = new SortedList<string, List<Action<TData>>>();
        private readonly IList<Action<string, TData>> _generalListeners = new List<Action<string, TData>>();

        /// <summary>
        /// Gets or sets the Pusher error handler.
        /// </summary>
        public Action<PusherException> ErrorHandler { get; set; }

        /// <summary>
        /// Gets whether any listeners are listening.
        /// </summary>
        public bool HasListeners
        {
            get
            {
                return _listeners.Count > 0 || _generalListeners.Count > 0;
            }
        }

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
                lock (_sync)
                {
                    if (!_listeners.ContainsKey(eventName))
                    {
                        _listeners[eventName] = new List<Action<TData>>();
                    }

                    if (!_listeners[eventName].Contains(listener))
                    {
                        _listeners[eventName].Add(listener);
                    }
                }
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
                lock (_sync)
                {
                    if (!_generalListeners.Contains(listener))
                    {
                        _generalListeners.Add(listener);
                    }
                }
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
                lock (_sync)
                {
                    if (_listeners.ContainsKey(eventName))
                    {
                        _listeners[eventName].Remove(listener);
                    }
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

            lock (_sync)
            {
                if (_listeners.ContainsKey(eventName))
                {
                    _listeners[eventName].Clear();
                    _listeners.Remove(eventName);
                }
            }
        }

        /// <summary>
        /// Unbinds a listener that listens to all events.
        /// </summary>
        /// <param name="listener">The listener to unbind.</param>
        public void Unbind(Action<string, TData> listener)
        {
            if (listener != null)
            {
                lock (_sync)
                {
                    _generalListeners.Remove(listener);
                }
            }
        }

        /// <summary>
        /// Removes all bindings.
        /// </summary>
        public void UnbindAll()
        {
            lock (_sync)
            {
                _generalListeners.Clear();
                foreach (var list in _listeners.Values)
                {
                    list.Clear();
                }

                _listeners.Clear();
            }
        }

        /// <summary>
        /// Emits an event.
        /// </summary>
        /// <param name="eventName">The name of the event to emit.</param>
        /// <param name="data">The event data.</param>
        public void EmitEvent(string eventName, TData data)
        {
            if (HasListeners)
            {
                List<Action<string, TData>> generalListeners = new List<Action<string, TData>>();
                List<Action<TData>> listeners = new List<Action<TData>>();
                lock (_sync)
                {

                    if (_generalListeners.Count > 0)
                    {
                        foreach (var action in _generalListeners)
                        {
                            generalListeners.Add(action);
                        }
                    }

                    if (_listeners.Count > 0)
                    {
                        if (_listeners.ContainsKey(eventName))
                        {
                            listeners.AddRange(_listeners[eventName]);
                        }
                    }
                }

                foreach (var action in generalListeners)
                {
                    try
                    {
                        action(eventName, data);
                    }
                    catch (Exception e)
                    {
                        HandleException(e, eventName, data);
                    }
                }

                foreach (var action in listeners)
                {
                    try
                    {
                        action(data);
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

                ErrorHandler.Invoke(errorToHandle);
            }
        }
    }
}
