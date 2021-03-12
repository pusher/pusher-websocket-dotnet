using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// The event handler for when a member is removed from a <see cref="GenericPresenceChannel{T}"/> or a <see cref="PresenceChannel"/>.
    /// </summary>
    /// <typeparam name="T">The detail of the member removed.</typeparam>
    /// <param name="sender">
    /// The <see cref="GenericPresenceChannel{T}"/> or <see cref="PresenceChannel"/> that had the member removed.
    /// </param>
    /// <param name="member">
    /// A <see cref="KeyValuePair{TKey, TValue}"/> where <c>TKey</c> is the user ID and <c>TValue</c> is the member detail removed.
    /// </param>
    public delegate void MemberRemovedEventHandler<T>(object sender, KeyValuePair<string, T> member);
}