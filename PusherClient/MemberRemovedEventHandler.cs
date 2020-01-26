using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// The Event Handler for the Member Removed Event on the <see cref="PresenceChannel"/>
    /// </summary>
    /// <param name="sender">The Channel that had the member removed</param>
    /// <param name="member">The removed member information</param>
    public delegate void MemberRemovedEventHandler<T>(object sender, KeyValuePair<string, T> member);
}