using System;
using System.Threading.Tasks;

namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// A latency inducer.
    /// </summary>
    public class LatencyInducer : ILatencyInducer
    {
        /// <summary>
        /// Gets or sets whether this latency inducer is enabled.
        /// </summary>
        public bool Enabled { get; set; } = Config.EnableAuthorizationLatency;

        /// <summary>
        /// If enabled, pauses for a random period between <paramref name="minLatency"/> and <paramref name="maxLatency"/>.
        /// </summary>
        /// <param name="minLatency">The minimum latency to induce measured in milli-seconds.</param>
        /// <param name="maxLatency">The maximum latency to induce measured in milli-seconds.</param>
        /// <returns>The number of milli-seconds that was induced. Will return 0 if disabled.</returns>
        public int InduceLatency(int minLatency, int maxLatency)
        {
            return InduceLatencyAsync(minLatency, maxLatency).Result;
        }

        /// <summary>
        /// If enabled, pauses for a random period between <paramref name="minLatency"/> and <paramref name="maxLatency"/>.
        /// </summary>
        /// <param name="minLatency">The minimum latency to induce measured in milli-seconds.</param>
        /// <param name="maxLatency">The maximum latency to induce measured in milli-seconds.</param>
        /// <returns>A task that can be awaited on.</returns>
        public async Task<int> InduceLatencyAsync(int minLatency, int maxLatency)
        {
            int result = 0;
            if (this.Enabled)
            {
                TimeSpan pause = TimeSpan.FromMilliseconds(Random.Next(minLatency, maxLatency + 1));
                result = pause.Milliseconds;
                await Task.Delay(pause).ConfigureAwait(false);
            }

            return result;
        }

        private static Random Random { get; } = new Random(42);
    }
}
