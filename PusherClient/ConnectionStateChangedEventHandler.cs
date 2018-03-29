namespace PusherClient
{
    /// <summary>
    /// The Event Handler for the Connection State Changed Event on the <see cref="Pusher"/> object
    /// </summary>
    /// <param name="sender">The object that subscribed</param>
    /// <param name="state">The new state</param>
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionState state);
}