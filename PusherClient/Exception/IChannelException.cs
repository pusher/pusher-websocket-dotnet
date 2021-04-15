namespace PusherClient
{
    public interface IChannelException
    {
        /// <summary>
        /// Gets or sets the name of the channel for which the exception occured.
        /// </summary>
        string ChannelName { get; set; }

        /// <summary>
        /// Gets or sets the event name for which the exception occured. Note that this property is not always available and can be null.
        /// </summary>
        string EventName { get; set; }

        /// <summary>
        /// Gets or sets the channel socket ID.
        /// </summary>
        string SocketID { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Channel"/> that errored if available.
        /// Note that this value can be null because the Channel object is not always available.
        /// </summary>
        Channel Channel { get; set; }
    }
}
