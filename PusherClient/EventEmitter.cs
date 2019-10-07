using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<string, List<Action<PusherEvent>>> _pusherEventEventListeners = new Dictionary<string, List<Action<PusherEvent>>>();
        private readonly List<Action<string, PusherEvent>> _pusherEventGeneralListeners = new List<Action<string, PusherEvent>>();

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
        /// Binds to a given event name. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<PusherEvent> listener)
        {
            if (_pusherEventEventListeners.ContainsKey(eventName))
            {
                _pusherEventEventListeners[eventName].Add(listener);
            }
            else
            {
                var listeners = new List<Action<PusherEvent>> { listener };
                _pusherEventEventListeners.Add(eventName, listeners);
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
        /// Binds to ALL event. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, PusherEvent> listener)
        {
            _pusherEventGeneralListeners.Add(listener);
        }

        /// <summary>
        /// Removes the binding for the given event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        public void Unbind(string eventName)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners.Remove(eventName);
            }

            if (_rawEventListeners.ContainsKey(eventName))
            {
                _rawEventListeners.Remove(eventName);
            }

            if (_pusherEventEventListeners.ContainsKey(eventName))
            {
                _pusherEventEventListeners.Remove(eventName);
            }
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
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<PusherEvent> listener)
        {
            if (_pusherEventEventListeners.ContainsKey(eventName))
            {
                _pusherEventEventListeners[eventName].Remove(listener);
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

            _pusherEventEventListeners.Clear();
            _pusherEventGeneralListeners.Clear();
        }

        internal void EmitEvent(string eventName, PusherEvent data)
        {
            var stringData = data.ToString();
            
            ActionData(_rawGeneralListeners, _rawEventListeners, eventName, stringData);
            EmitDynamicEvent(eventName, stringData);
            ActionData(_pusherEventGeneralListeners, _pusherEventEventListeners, eventName, data);
        }

        internal void EmitDynamicEvent(string eventName, string data)
        {
            if (_generalListeners.Count > 0 || _eventListeners.Count > 0)
            {
                var dynamicData = JsonConvert.DeserializeObject<dynamic>(data);
                ActionData(_generalListeners, _eventListeners, eventName, dynamicData);
            }
        }

        private void ActionData<T>(List<Action<string, T>> listToProcess, Dictionary<string, List<Action<T>>> dictionaryToProcess, string eventName, T data)
        {
            if (listToProcess.Count > 0 || dictionaryToProcess.Count > 0)
            {
                listToProcess.ForEach(a => a(eventName, data));

                if (dictionaryToProcess.ContainsKey(eventName))
                {
                    dictionaryToProcess[eventName].ForEach(a => a(data));
                }
            }
        }
    }
}