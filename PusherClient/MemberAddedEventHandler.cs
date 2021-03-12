using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// The event handler for when a member is added to a <see cref="GenericPresenceChannel{T}"/> or a <see cref="PresenceChannel"/>.
    /// </summary>
    /// <typeparam name="T">The detail of the member added.</typeparam>
    /// The <see cref="GenericPresenceChannel{T}"/> or <see cref="PresenceChannel"/> that had the member added.
    /// <param name="member">
    /// A <see cref="KeyValuePair{TKey, TValue}"/> where <c>TKey</c> is the user ID and <c>TValue</c> is the member detail added.
    /// </param>
    public delegate void MemberAddedEventHandler<T>(object sender, KeyValuePair<string, T> member);
}