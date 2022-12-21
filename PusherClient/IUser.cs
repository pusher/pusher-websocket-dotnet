namespace PusherClient
{
    /// <summary>
    /// Contract for an object that represents a user on a Pusher connection.
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Return the user id of the signed in user
        /// </summary>
        /// <returns>The user id of the signed in user.</returns>
        string UserID();

        /// <summary>
        /// Binds a listener to an event. 
        /// The listenerwill be notified whenever the specified event is received for this user.
        /// </summary>
        /// <param name="eventName">The name of the event to listen to</param>
        /// <param name="listener">A listener to receive notifications when the event is received</param>
        /// <returns></returns>
        void Bind(String eventName, IEventBinder<TData> listener);

        /// <summary>
        /// Binds a listener to all events. 
        /// The listener will be notified whenever an event is received for this user.
        /// </summary>
        /// <param name="listener">A listener to receive notifications when the event is received</param>
        /// <returns></returns>
        void BindAll(IEventBinder<TData> listener);

        /// <summary>
        /// Unbinds a previously bound listener from an event. 
        /// The listener will no longer be notified whenever the specified event is received for this user.
        /// </summary>
        /// <param name="eventName">The name of the event to stop listening to</param>
        /// <param name="listener">The listener to unbind from the event</param>
        /// <returns></returns>
        void Unbind(String eventName, IEventBinder<TData> listener);

        /// <summary>
        /// Unbinds a previously bound listener from global events.
        /// The listener will no longer be notified whenever the any event is received for this user.
        /// </summary>
        /// <param name="listener">The listener to unbind from the event</param>
        /// <returns></returns>
        void UnbindAll(IEventBinderr<TData> listener);
    }
}