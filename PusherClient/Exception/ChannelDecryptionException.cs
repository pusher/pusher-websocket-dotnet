using System;

namespace PusherClient
{
    /// <summary>
    /// This exception is raised when the private encrypted channel data could not be decrypted.
    /// </summary>
    public class ChannelDecryptionException : ChannelException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ChannelDecryptionException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ChannelDecryptionException(string message)
            : base(message, ErrorCodes.ChannelDecryptionFailure, null, null)
        {
        }
    }
}
