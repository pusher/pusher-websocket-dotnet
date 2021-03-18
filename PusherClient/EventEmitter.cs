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
        private IDictionary<string, IEventBinder> Emitters { get; } = new SortedList<string, IEventBinder>
        {
            {nameof(PusherEventEmitter), new PusherEventEmitter() },
            {nameof(TextEventEmitter), new TextEventEmitter() },
            {nameof(DynamicEventEmitter), new DynamicEventEmitter() },
        };

        /// <summary>
        /// Binds to a given event name
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<dynamic> listener)
        {
            ((DynamicEventEmitter)Emitters[nameof(DynamicEventEmitter)]).Bind(eventName, listener);
        }

        /// <summary>
        /// Binds to a given event name. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<string> listener)
        {
            ((TextEventEmitter)Emitters[nameof(TextEventEmitter)]).Bind(eventName, listener);
        }

        /// <summary>
        /// Binds to a given event name. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="eventName">The Event Name to listen for</param>
        /// <param name="listener">The action to perform when the event occurs</param>
        public void Bind(string eventName, Action<PusherEvent> listener)
        {
            ((PusherEventEmitter)Emitters[nameof(PusherEventEmitter)]).Bind(eventName, listener);
        }

        /// <summary>
        /// Binds to ALL event
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, dynamic> listener)
        {
            ((DynamicEventEmitter)Emitters[nameof(DynamicEventEmitter)]).Bind(listener);
        }

        /// <summary>
        /// Binds to ALL event. The listener will receive the raw JSON message.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, string> listener)
        {
            ((TextEventEmitter)Emitters[nameof(TextEventEmitter)]).Bind(listener);
        }

        /// <summary>
        /// Binds to ALL event. The listener will receive a Pusher Event.
        /// </summary>
        /// <param name="listener">The action to perform when the any event occurs</param>
        public void BindAll(Action<string, PusherEvent> listener)
        {
            ((PusherEventEmitter)Emitters[nameof(PusherEventEmitter)]).Bind(listener);
        }

        /// <summary>
        /// Removes the binding for the given event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        public void Unbind(string eventName)
        {
            foreach (IEventBinder binder in Emitters.Values)
            {
                binder.Unbind(eventName);
            }
        }

        /// <summary>
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<dynamic> listener)
        {
            ((DynamicEventEmitter)Emitters[nameof(DynamicEventEmitter)]).Unbind(eventName, listener);
        }

        /// <summary>
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<string> listener)
        {
            ((TextEventEmitter)Emitters[nameof(TextEventEmitter)]).Unbind(eventName, listener);
        }

        /// <summary>
        /// Remove the action for the event name
        /// </summary>
        /// <param name="eventName">The name of the event to unbind</param>
        /// <param name="listener">The action to remove</param>
        public void Unbind(string eventName, Action<PusherEvent> listener)
        {
            ((PusherEventEmitter)Emitters[nameof(PusherEventEmitter)]).Unbind(eventName, listener);
        }

        /// <summary>
        /// Remove All bindings
        /// </summary>
        public void UnbindAll()
        {
            foreach (IEventBinder binder in Emitters.Values)
            {
                binder.UnbindAll();
            }
        }

        internal IEventBinder GetEventBinder(string eventBinderKey)
        {
            return Emitters[eventBinderKey];
        }

        protected void SetEventEmitterErrorHandler(Action<PusherException> errorHandler)
        {
            foreach (IEventBinder binder in Emitters.Values)
            {
                binder.ErrorHandler = errorHandler;
            }
        }
    }
}