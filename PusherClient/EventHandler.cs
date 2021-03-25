using System.Collections.Generic;

namespace PusherClient
{
    /// <summary>
    /// The delegate to handle the <c>Pusher.Connected</c> event.
    /// </summary>
    /// <param name="sender">The <see cref="Pusher"/> client that connected.</param>
    public delegate void ConnectedEventHandler(object sender);

    /// <summary>
    /// The delegate to handle the <c>Pusher.ConnectionStateChanged</c> event.
    /// </summary>
    /// <param name="sender">The <see cref="Pusher"/> client that's state changed.</param>
    /// <param name="state">The new state.</param>
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionState state);

    /// <summary>
    /// The delegate to handle the <c>Pusher.Error</c> event.
    /// </summary>
    /// <param name="sender">The <see cref="Pusher"/> client that errored.</param>
    /// <param name="error">The error that occured.</param>
    public delegate void ErrorEventHandler(object sender, PusherException error);

    /// <summary>
    /// The delegate for when a member is added to a <see cref="GenericPresenceChannel{T}"/> or a <see cref="PresenceChannel"/>.
    /// </summary>
    /// <typeparam name="T">The detail of the member added.</typeparam>
    /// <param name="sender">
    /// The <see cref="GenericPresenceChannel{T}"/> or <see cref="PresenceChannel"/> that had the member added.
    /// </param>
    /// <param name="member">
    /// A <see cref="KeyValuePair{TKey, TValue}"/> where <c>TKey</c> is the user ID and <c>TValue</c> is the member detail added.
    /// </param>
    public delegate void MemberAddedEventHandler<T>(object sender, KeyValuePair<string, T> member);

    /// <summary>
    /// The delegate for when a member is removed from a <see cref="GenericPresenceChannel{T}"/> or a <see cref="PresenceChannel"/>.
    /// </summary>
    /// <typeparam name="T">The detail of the member removed.</typeparam>
    /// <param name="sender">
    /// The <see cref="GenericPresenceChannel{T}"/> or <see cref="PresenceChannel"/> that had the member removed.
    /// </param>
    /// <param name="member">
    /// A <see cref="KeyValuePair{TKey, TValue}"/> where <c>TKey</c> is the user ID and <c>TValue</c> is the member detail removed.
    /// </param>
    public delegate void MemberRemovedEventHandler<T>(object sender, KeyValuePair<string, T> member);

    /// <summary>
    /// The delegate to handle the <c>Pusher.Subscribed</c> event.
    /// </summary>
    /// <param name="sender">The <see cref="Pusher"/> client that has had a channel subcribed to.</param>
    /// <param name="channel">The channel that has been subscribed to.</param>
    public delegate void SubscribedEventHandler(object sender, Channel channel);

    /// <summary>
    /// The delegate to handle the <c>Channel.Subscribed</c> event.
    /// </summary>
    /// <param name="sender">The <see cref="Channel"/> that subscribed</param>
    /// <remarks>
    /// To be deprecated, please use <see cref="SubscribedEventHandler"/> instead.
    /// </remarks>
    public delegate void SubscriptionEventHandler(object sender);
}