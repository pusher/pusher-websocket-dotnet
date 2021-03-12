using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// Interface for a presence channel.
    /// </summary>
    /// <typeparam name="T">Channel member detail.</typeparam>
    public interface IPresenceChannel<T>
    {
        /// <summary>
        /// Gets a member using the member's user ID.
        /// </summary>
        /// <param name="userId">The member's user ID.</param>
        /// <returns>Retruns the member if found; otherwise returns null.</returns>
        T GetMember(string userId);

        /// <summary>
        /// Gets the current list of members as a <see cref="Dictionary{TKey, TValue}"/> where the TKey is the user ID and TValue is the member detail.
        /// </summary>
        /// <returns>Returns a <see cref="Dictionary{TKey, TValue}"/> containing the current members.</returns>
        Dictionary<string, T> GetMembers();
    }
}