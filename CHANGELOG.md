# Changelog

## 2.2.1
* [CHANGED] Update dependency Newtonsoft.Json 13.0.2

## 2.2.0
* [Added] Support for subscription_count handler

## 2.1.0
* [ADDED] Strong name to the PusherClient assembly.
* [ADDED] Support for the authentication header on the HttpAuthorizer.
* [ADDED] End-to-end encryption for private encrypted channels.
* [ADDED] Method Channel.UnsubscribeAsync.
* [ADDED] Host to PusherOptions.
* [FIXED] The intermittent WebsocketAutoReconnect issue The socket is connecting, cannot connect again!

## 2.0.1
* [FIXED] Filter on event name in event emitter.

## 2.0.0
* [FIXED] Infinite loop when failing to connect for the first time. 
* [FIXED] Bug: GenericPresenceChannel<T>'s AddMember and RemoveMember events were not being emitted.
* [FIXED] Change MemberRemovedEventHandler to MemberRemovedEventHandler<T>.
* [FIXED] Introduce new ChannelUnauthorizedException class.
* [FIXED] Bug: calling Channel.Unsubscribe would only set IsSubscribed to false and not remove the channel subscription.
* [FIXED] Bug: Pusher can error if any of the delegate events raise errors.
* [FIXED] Bug: PusherEvent.Data fails to serialize for types other than string.
* [FIXED] Concurrency issues in EventEmmiter.
* [FIXED] Failing tests for Pusher apps in a cluster other than the default.
* [FIXED] Issues in the Example app and removed the use of dynamic types.
* [CHANGED] PusherClient project structure to target .NET 4.5, .NET 4.7.2, .NET Standard 1.3 and .NET Standard 2.0.
* [CHANGED] The events emitted by Pusher.ConnectionStateChanged. Connecting and Connected are new. Initialized has been removed. Disconnecting, Disconnected and WaitingToReconnect remain unchanged.
* [CHANGED] Pusher methods ConnectAsync and DisconnectAsync to return void instead of ConnectionState.
* [CHANGED] Pusher.SubscribeAsync to take an optional SubscriptionEventHandler parameter.
* [CHANGED] Separate PusherEvent, string and dynamic emitters into separate classes.
* [CHANGED] Bump PusherServer version to 4.4.0.
* [CHANGED] PusherClient project structure to target .NET 4.5, .NET 4.7.2, .NET Standard 1.3 and .NET Standard 2.0.
* [REMOVED] public property Pusher.Channels; it is now private.
* [REMOVED] public property GenericPresenceChannel<T>.Members; it is now private.
* [REMOVED] The following ConnectionState values: Initialized, NotConnected, AlreadyConnected, ConnectionFailed and DisconnectionFailed.
* [REMOVED] public Channel.Subscribed; it is now internal.
* [REMOVED] Pusher.Trace property.
* [ADDED] To Pusher class: methods UnsubscribeAsync, UnsubscribeAllAsync, GetChannel and GetAllChannels; event delegate Subscribed.
* [ADDED] To GenericPresenceChannel<T> class: methods GetMember and GetMembers.
* [ADDED] ClientTimeout to PusherOptions and implemented client timeouts with tests.
* [ADDED] Json config file for test application settings.
* [ADDED] Client side event triggering validation.
* [ADDED] ITraceLogger interface and TraceLogger class, tracing is disabled by default. New TraceLogger property added to PusherOptions.

## 1.1.2
* [FIX] Switch to concurrent collections to avoid race in EventEmitter (issue #76, PR #93)
* [FIX] Fix Reconnection issue and NRE on Disconnect() after websocket_Closed (issue #70, issue #71, issue #73, PR #95)
* [FIX] Reset `_backOffMillis` after a successful reconnection (issue #97, PR #96)

## 1.1.1
* [FIX] Removed extra double quotes from PusherEvent.Data string (PR #84)
* [FIX] Fixed JsonReaderException in the HttpAuthorizer (issue #78, issue #85, PR #86)

## 1.1.0
* [FIX] Mitigates NRE and race in Connect/Disconnect (PR #72, issue #71)
* [FIX] Potential incompatibility with il2cpp compiler for iOS target due to dynamic keyword (issue #69)
* [FIX] Race condition in PresenceChannel name (issue #44)
* [ADDED] Extended API for Bind/Unbind and BindAll/UnbindAll to emit a more idiomatic <PusherEvent> as an alternative to the raw data String
* [ADDED] NUnit 3 Test Adaptor to enable test integration with VS2019

## 1.0.2
* [CHANGED] Project now targets DotNet Standard 1.6.
* [REMOVED] Retired Sync versions of method. Updated tests to use async versions of methods.

## 0.5.1
* [FIXED] Potential crash when a channel is added while a connection state change is being handled.
* [CHANGED] Log an error rather than throw an exception if an error occurs on a connection.

## 0.5.0
* [CHANGED] After calling Connect(), subsequent calls will no-op unless Disconnect() is called first.
* [CHANGED] Event listeners are removed from _websocket and _connection on disconnect

## 0.4.0
* [CHANGED] MemberAdded event handler is now aware of *which* member has been added
* [ADDED] Unbind methods
* [ADDED] Ability to change cluster (thanks @Misiu)
* [CHANGED] Update dependencies
  * Newtonsoft.Json 9.0.1
  * WebSocket4Net 0.14.1

## 0.3.0

* [ADDED] Auto re-subscription
* [ADDED] Exceptions are raised via an event and aren't thrown (unless there are no event handlers)
* [ADDED] Auto-reconnections now backs off by 1 second increments to a maximum of a retry every 10 seconds.
* [ADDED] The WebSocket Host can be configured
* [ADDED] You can now subscribe prior to connecting
* [REMOVED] `Pusher.Send` method on the public API
* [CHANGED] Update dependencies
  * Newtonsoft.Json 7.0.1
  * WebSocket4Net 0.13.1

## 0.2.0

* [CHANGED] Update package dependencies
  * Newtonsoft.Json 6.0.4
  * WebSocket4Net 0.10
