using System;
using System.Diagnostics;

namespace PusherClient.Tests.Utilities
{
    public class FakeUnauthoriser : IAuthorizer
    {
        public FakeUnauthoriser()
        {
        }

        public string Authorize(string channelName, string socketId)
        {
            double delay = LatencyInducer.InduceLatency(200, 2500) / 1000.0;
            Trace.TraceInformation($"{this.GetType().Name} paused for {Math.Round(delay, 3)} second(s)");
            throw new ChannelUnauthorizedException(channelName, socketId);
        }

        private static ILatencyInducer LatencyInducer { get; } = new LatencyInducer();
    }
}
