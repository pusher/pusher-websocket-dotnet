using System.Diagnostics;

namespace PusherClient
{
    /// <summary>
    /// Used to trace debug messages.
    /// </summary>
    public class TraceLogger : ITraceLogger
    {
        private static TraceSource Trace { get; } = new TraceSource(nameof(Pusher));

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        public void TraceError(string message)
        {
            if (message != null)
            {
                Trace.TraceEvent(TraceEventType.Error, -1, message);
            }
        }

        /// <summary>
        /// Traces an information message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        public void TraceInformation(string message)
        {
            if (message != null)
            {
                Trace.TraceEvent(TraceEventType.Information, 0, message);
            }
        }

        /// <summary>
        /// Traces a warning message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        public void TraceWarning(string message)
        {
            if (message != null)
            {
                Trace.TraceEvent(TraceEventType.Warning, 1, message);
            }
        }
    }
}
