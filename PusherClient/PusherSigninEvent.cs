namespace PusherClient
{
    internal class PusherSigninEvent : PusherSystemEvent
    {
        public PusherSigninEvent(PusherSigninEventData data)
            : base(Constants.PUSHER_SIGNIN, data)
        {
        }
    }
}
