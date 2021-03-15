using System;
using System.Diagnostics;

namespace PusherClient.Tests.Utilities
{
    public class FakeUnauthoriser : IAuthorizer
    {
        public const string UnauthoriseToken = "-unauth";

        public FakeUnauthoriser()
        {
        }

        public string Authorize(string channelName, string socketId)
        {
            double delay = LatencyInducer.InduceLatency(FakeAuthoriser.MinLatency, FakeAuthoriser.MaxLatency) / 1000.0;
            Trace.TraceInformation($"{this.GetType().Name} paused for {Math.Round(delay, 3)} second(s)");
            if (channelName.Contains(UnauthoriseToken))
            {
                throw new ChannelUnauthorizedException($"https://localhost/{nameof(FakeUnauthoriser)}", channelName, socketId);
            }
            else
            {
                throw new ChannelAuthorizationFailureException("Endpoint not found.", ErrorCodes.ChannelAuthorizationError, $"https://does.not.exist/{nameof(FakeUnauthoriser)}", channelName, socketId);
            }
        }

        private static ILatencyInducer LatencyInducer { get; } = new LatencyInducer();
    }
}
