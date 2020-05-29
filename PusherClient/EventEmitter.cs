using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, List<Action<dynamic>>> _eventListeners = new ConcurrentDictionary<string, List<Action<dynamic>>>();
        private readonly ConcurrentStack<Action<string, dynamic>> _generalListeners = new ConcurrentStack<Action<string, dynamic>>();

        private readonly ConcurrentDictionary<string, List<Action<string>>> _rawEventListeners = new ConcurrentDictionary<string, List<Action<string>>>(); 
        private readonly ConcurrentStack<Action<string, string>> _rawGeneralListeners = new ConcurrentStack<Action<string, string>>();

        private readonly ConcurrentDictionary<string, List<Action<PusherEvent>>> _pusherEventEventListeners = new ConcurrentDictionary<string, List<Action<PusherEvent>>>();
        private readonly ConcurrentStack<Action<string, PusherEvent>> _pusherEventGeneralListeners = new ConcurrentStack<Action<string, PusherEvent>>();

        /// <summary>
        /// Binds to a given event name
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<dynamic> listener)
        {
            var listeners = new List<Action<dynamic>> { listener };

            if (!_eventListeners.TryAdd(eventName, listeners))
            {
                _eventListeners[eventName].Add(listener);
            }
        }

        /// <summary>
        /// Binds to a given event name. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<string> listener)
        {
            var listeners = new List<Action<string>> { listener };

            if (!_rawEventListeners.TryAdd(eventName, listeners))
            {
                _rawEventListeners[eventName].Add(listener);
            }
        }

        /// <summary>
        /// Binds to a given event name. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<PusherEvent> listener)
        {
            var listeners = new List<Action<PusherEvent>> { listener };

            if (!_pusherEventEventListeners.TryAdd(eventName, listeners))
            {
                _pusherEventEventListeners[eventName].Add(listener);
            }
        }

        /// <summary>
        /// Binds to ALL event
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, dynamic> listener)
        {
            _generalListeners.Push(listener);
        }

        /// <summary>
        /// Binds to ALL event. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, string> listener)
        {
            _rawGeneralListeners.Push(listener);
        }

        /// <summary>
        /// Binds to ALL event. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, PusherEvent> listener)
        {
            _pusherEventGeneralListeners.Push(listener);
        }

        /// <summary>
        /// Removes the binding for the given event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        public void Unbind(string eventName)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                List<Action<dynamic>> outEventValue = null;
                _eventListeners.TryRemove(eventName, out outEventValue);

            }

            if (_rawEventListeners.ContainsKey(eventName))
            {
                List<Action<string>> outRawValue = null;
                _rawEventListeners.TryRemove(eventName, out outRawValue);
            }

            if (_pusherEventEventListeners.ContainsKey(eventName))
            {
                List<Action<PusherEvent>> outPusherValue = null;
                _pusherEventEventListeners.TryRemove(eventName, out outPusherValue);
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
            //No data, no event!
            if(data == null)
            {
                return;
            }
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

        private void ActionData<T>(ConcurrentStack<Action<string, T>> listToProcess, ConcurrentDictionary<string, List<Action<T>>> dictionaryToProcess, string eventName, T data)
        {
            if (listToProcess.Count > 0 || dictionaryToProcess.Count > 0)
            {
                foreach (var a in listToProcess)
                {
                    a(eventName, data);
                }

                if (dictionaryToProcess.ContainsKey(eventName))
                {
                    dictionaryToProcess[eventName].ForEach(a => a(data));
                }
            }
        }
    }
}