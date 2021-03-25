using System;

namespace PusherClient
{
    /// <summary>
    /// An instance of this class gets passed to the Pusher Error delegate when the MemberAdded delegate raises an unexpected exception.
    /// </summary>
    /// <typeparam name="T">The member detail.</typeparam>
    public class MemberAddedEventHandlerException<T> : EventHandlerException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="MemberAddedEventHandlerException"/> class.
        /// </summary>
        /// <param name="memberKey">The key for the member that caused this exception.</param>
        /// <param name="member">The detail of the member.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MemberAddedEventHandlerException(string memberKey, T member, Exception innerException)
            : base($"Error invoking the MemberAdded delegate:{Environment.NewLine}{innerException.Message}", ErrorCodes.MemberAddedEventHandlerError, innerException)
        {
            this.MemberKey = memberKey;
            this.Member = member;
        }

        /// <summary>
        /// Gets the key for the member that caused this exception.
        /// </summary>
        public string MemberKey { get; private set; }

        /// <summary>
        /// Gets the detail of the member.
        /// </summary>
        public T Member { get; private set; }
    }
}
