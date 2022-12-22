using System.Threading.Tasks;

namespace PusherClient
{
    public interface IUserFacade : IEventBinder<UserEvent>
    {
        Task SigninAsync();

        // public WatchlistFacade Watchlist();

    }
}