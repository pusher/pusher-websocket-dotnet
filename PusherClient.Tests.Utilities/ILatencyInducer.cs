using System.Threading.Tasks;

namespace PusherClient.Tests.Utilities
{
    /// <summary>
    /// Interface for a latency inducer.
    /// </summary>
    public interface ILatencyInducer
    {
        /// <summary>
        /// Gets or sets whether this latency inducer is enabled.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// If enabled, pauses for a random period between <paramref name="minLatency"/> and <paramref name="maxLatency"/>.
        /// </summary>
        /// <param name="minLatency">The minimum latency to induce measured in milli-seconds.</param>
        /// <param name="maxLatency">The maximum latency to induce measured in milli-seconds.</param>
        /// <returns>The number of milli-seconds that was induced. Will return 0 if disabled.</returns>
        int InduceLatency(int minLatency, int maxLatency);

        /// <summary>
        /// If enabled, pauses for a random period between <paramref name="minLatency"/> and <paramref name="maxLatency"/>.
        /// </summary>
        /// <param name="minLatency">The minimum latency to induce measured in milli-seconds.</param>
        /// <param name="maxLatency">The maximum latency to induce measured in milli-seconds.</param>
        /// <returns>The number of milli-seconds that was induced. Will return 0 if disabled.</returns>
        Task<int> InduceLatencyAsync(int minLatency, int maxLatency);
    }
}
