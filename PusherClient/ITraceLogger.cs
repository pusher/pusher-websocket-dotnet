namespace PusherClient
{
    public interface ITraceLogger
    {
        /// <summary>
        /// Traces an information message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        void TraceInformation(string message);

        /// <summary>
        /// Traces a warning message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        void TraceWarning(string message);

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="message">The message to trace.</param>
        void TraceError(string message);
    }
}
