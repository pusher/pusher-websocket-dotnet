using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PusherClient
{
    /// <summary>
    /// Class used to Bind and unbind from events
    /// </summary>
    public class EventEmitter
    {
        private readonly Dictionary<string, List<Action<dynamic>>> _eventListeners = new Dictionary<string, List<Action<dynamic>>>();
        private readonly List<Action<string, dynamic>> _generalListeners = new List<Action<string, dynamic>>();

        private readonly Dictionary<string, List<Action<string>>> _rawEventListeners = new Dictionary<string, List<Action<string>>>();
        private readonly List<Action<string, string>> _rawGeneralListeners = new List<Action<string, string>>();

        /// <summary>
        /// Binds to a given event name
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<dynamic> listener)
        {
            if(_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Add(listener);
            }
            else
            {
                var listeners = new List<Action<dynamic>> {listener};
                _eventListeners.Add(eventName, listeners);
            }
        }

        /// <summary>
        /// Binds to a given event name. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<string> listener)
        {
            if (_rawEventListeners.ContainsKey(eventName))
            {
                _rawEventListeners[eventName].Add(listener);
            }
            else
            {
                var listeners = new List<Action<string>> { listener };
                _rawEventListeners.Add(eventName, listeners);
            }
        }

        /// <summary>
        /// Binds to ALL event
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, dynamic> listener)
        {
            _generalListeners.Add(listener);
        }

        /// <summary>
        /// Binds to ALL event. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, string> listener)
        {
            _rawGeneralListeners.Add(listener);
        }

        /// <summary>
        /// Removes the binding for the given event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        public void Unbind(string eventName)
        {
            _eventListeners.Remove(eventName);
            _rawEventListeners.Remove(eventName);
        }

        /// <summary>
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<dynamic> listener)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Remove(listener);
            }
        }

        /// <summary>
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<string> listener)
        {
            if (_rawEventListeners.ContainsKey(eventName))
            {
                _rawEventListeners[eventName].Remove(listener);
            }
        }

        /// <summary>
        /// Remove All bindings
        /// </summary>
        public void UnbindAll()
        {
            _eventListeners.Clear();
            _generalListeners.Clear();

            _rawEventListeners.Clear();
            _rawGeneralListeners.Clear();
        }

        internal void EmitEvent(string eventName, string data)
        {
            foreach (var action in _rawGeneralListeners)
            {
                action(eventName, data);
            }

            if (_rawEventListeners.ContainsKey(eventName))
            {
                foreach (var action in _rawEventListeners[eventName])
                {
                    action(data);
                }
            }

            // Don't bother with deserialization if there are no dynamic listeners
            if (_generalListeners.Count > 0 || _eventListeners.Count > 0)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(data);

                foreach (var action in _generalListeners)
                {
                    action(eventName, obj);
                }

                if (_eventListeners.ContainsKey(eventName))
                {
                    foreach (var action in _eventListeners[eventName])
                    {
                        action(obj);
                    }
                }
            }
        }
    }
}