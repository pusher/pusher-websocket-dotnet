using System.Threading.Tasks;

namespace PusherClient.Tests.Utilities
{
    public class FakeUnauthoriser : IAuthorizer, IAuthorizerAsync
    {
        public FakeUnauthoriser()
        {
        }

        public string Authorize(string channelName, string socketId)
        {
            throw new ChannelUnauthorizedException(channelName, socketId);
        }

        public async Task<string> AuthorizeAsync(string channelName, string socketId)
        {
            string result = null;
            await Task.Run(() =>
            {
                result = this.Authorize(channelName, socketId);
            }).ConfigureAwait(false);

            return result;
        }
    }
}
