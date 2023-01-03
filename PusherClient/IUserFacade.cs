using System.Threading.Tasks;

namespace PusherClient
{
    public interface IUserFacade : IEventBinder<UserEvent>
    {
        void Signin();
        Task SigninDoneAsync();
        IWatchlistFacade Watchlist { get; }

    }
}