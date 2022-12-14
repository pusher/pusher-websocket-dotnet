using System;
using System.Diagnostics;

namespace PusherClient.Tests.Utilities
{
    public class FakeChannelUnauthoriser : IChannelAuthorizer
    {
        public const string UnauthoriseToken = "-unauth";

        public FakeChannelUnauthoriser()
        {
        }

        public string Authorize(string channelName, string socketId)
        {
            double delay = LatencyInducer.InduceLatency(FakeChannelAuthoriser.MinLatency, FakeChannelAuthoriser.MaxLatency) / 1000.0;
            Trace.TraceInformation($"{this.GetType().Name} paused for {Math.Round(delay, 3)} second(s)");
            if (channelName.Contains(UnauthoriseToken))
            {
                throw new ChannelUnauthorizedException($"https://localhost/{nameof(FakeChannelUnauthoriser)}", channelName, socketId);
            }
            else
            {
                throw new ChannelAuthorizationFailureException("Endpoint not found.", ErrorCodes.ChannelAuthorizationError, $"https://does.not.exist/{nameof(FakeChannelUnauthoriser)}", channelName, socketId);
            }
        }

        private static ILatencyInducer LatencyInducer { get; } = new LatencyInducer();
    }
}
