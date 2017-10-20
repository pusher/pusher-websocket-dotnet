using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PusherClient
{
    public class EventEmitter
    {
        private Dictionary<string, List<Action<string, dynamic>>> _eventListeners = new Dictionary<string, List<Action<string, dynamic>>>();
        private List<Action<string, string, dynamic>> _generalListeners = new List<Action<string, string, dynamic>>();

        public void Bind(string eventName, Action<string, dynamic> listener)
        {
            if(_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Add(listener);
            }
            else
            {
                List<Action<string, dynamic>> listeners = new List<Action<string, dynamic>>();
                listeners.Add(listener);
                _eventListeners.Add(eventName, listeners);
            }
        }

        public void BindAll(Action<string, string, dynamic> listener)
        {
            _generalListeners.Add(listener);
        }

        public void Unbind(string eventName)
        {
            _eventListeners.Remove(eventName);
        }

        public void Unbind(string eventName, Action<string, dynamic> listener)
        {
            if(_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Remove(listener);
            }
        }

        public void UnbindAll()
        {
          _eventListeners.Clear();
          _generalListeners.Clear();
        }

        internal void EmitEvent(string eventName, string channel, string data)
        {
            // Channel is not always present when a message is received.
            channel = channel ?? "";
            
            var obj = JsonConvert.DeserializeObject<dynamic>(data);

            // Emit to general listeners regardless of event type
            foreach (var action in _generalListeners)
            {
                action(eventName, channel, obj);
            }

            if (_eventListeners.ContainsKey(eventName))
            {
                foreach (var action in _eventListeners[eventName])
                {
                    action(channel, obj);
                }
            }

        }
    }
}
