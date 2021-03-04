namespace PusherClient
{
    /// <summary>
    /// The event handler for channel Subscribed events on the <see cref="Pusher"/> class.
    /// </summary>
    /// <param name="sender">The object that subscribed.</param>
    /// <param name="channelName">The name of the channel that has been subscribed to.</param>
    public delegate void SubscribedEventHandler(object sender, string channelName);
}