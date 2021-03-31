using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets emitted to the Pusher Error delegate whenever an <see cref="EventEmitter{TEvent}"/> action raises an unexpected exception.
    /// </summary>
    /// <typeparam name="TData">The event data type.</typeparam>
    public class EventEmitterActionException<TData> : PusherException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="EventEmitterActionException"/> class.
        /// </summary>
        /// <param name="code">The Pusher error code.</param>
        /// <param name="eventName">The event name for the action that raised an error.</param>
        /// <param name="data">The data for the event action that raised the error.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public EventEmitterActionException(ErrorCodes code, string eventName, TData data, Exception innerException)
            : base($"Error invoking the action for the emitted event {eventName}:{Environment.NewLine}{innerException.Message}", code, innerException)
        {
            this.EventData = data;
            this.EventName = eventName;
        }

        /// <summary>
        /// Gets the data for the event action that raised the error.
        /// </summary>
        public TData EventData { get; private set; }

        /// <summary>
        /// Gets the event name for the action that raised an error.
        /// </summary>
        public string EventName { get; private set; }
    }
}
