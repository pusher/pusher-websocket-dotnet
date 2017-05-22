namespace PusherClient
{
    /// <summary>
    /// The Event Handler for the Error Event on the <see cref="Pusher"/>
    /// </summary>
    /// <param name="sender">The object that subscribed</param>
    /// <param name="error">The Error that occured</param>
    public delegate void ErrorEventHandler(object sender, PusherException error);
}