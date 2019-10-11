# Changelog

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

### 0.2.0

* [CHANGED] Update package dependencies
  * Newtonsoft.Json 6.0.4
  * WebSocket4Net 0.10
