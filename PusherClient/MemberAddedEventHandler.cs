using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// The Event Handler for the Member Added Event on the <see cref="PresenceChannel"/>
    /// </summary>
    /// <param name="sender">The Channel that had the member added</param>
    /// <param name="member">The added member information</param>
    public delegate void MemberAddedEventHandler(object sender, KeyValuePair<string, dynamic> member);
}