using System.Threading.Tasks;

namespace PusherClient
{
    internal interface IConnection
    {
        string SocketId { get; }

        ConnectionState State { get; }

        bool IsConnected { get; }

        Task ConnectAsync();

        Task DisconnectAsync();

        Task<bool> SendAsync(string message);
    }
}
