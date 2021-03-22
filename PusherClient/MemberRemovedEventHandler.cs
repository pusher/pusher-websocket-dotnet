using System.Collections.Generic;

namespace PusherClient
{
    /// The Event Handler for the Member Removed Event on the <see cref="PresenceChannel"/>
    /// </summary>
    /// <param name="sender">The Channel that had the member removed</param>
    /// <param name="member">The added member information</param>
    public delegate void MemberRemovedEventHandler<T>(object sender, KeyValuePair<string, T> member);
}