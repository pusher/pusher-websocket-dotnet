# Pusher Channels .NET Client library

This is a .NET library for interacting with the Channels WebSocket API.

Register at [pusher.com/channels](https://pusher.com/channels) and use the application credentials within your app as shown below.

More general documentation can be found on the [Official Channels Documentation](https://pusher.com/docs/channels).

For integrating **Pusher Channels** with **Unity** follow the instructions at <https://github.com/pusher/pusher-websocket-unity>

## Supported platforms

⚠️ we recently released a major version with many breaking changes! If you are coming from version 1.x, please see the [migration section](#migrating-from-version-1-to-version-2)

* .NET Standard 1.3
* .NET Standard 2.0
* .NET 4.5
* .NET 4.7.2
* Unity 2018 and greater via [pusher-websocket-unity](https://github.com/pusher/pusher-websocket-unity)

## TOC

Contents:

- [Installation](#installation)
- [API](#api)
  - [Overview](#overview)
  - [Sample application](#sample-application)
- [Configuration](#configuration)
  - [The PusherOptions object](#the-pusheroptions-object)
  - [Application Key](#application-key)
- [Connecting](#connecting)
  - [Connection States](#connection-States)
  - [Auto reconnect](#auto-reconnect)
  - [Disconnecting](#disconnecting)
  - [Connected and Disconnected delegates](#connected-and-disconnected-delegates)
- [Subscribing](#subscribing)
  - [Error handling](#error-handling)
  - [Public channels](#public-channels)
  - [Private channels](#private-channels)
  - [Presence channels](#Presence-channels)
  - [Subscribed delegate](#subscribed-delegate)
  - [Unsubscribe](#unsubscribe)
- [Binding to events](#binding-to-events)
  - [Per-channel](#per-channel)
  - [Globally](#globally)
- [Triggering events](#triggering-events)
- [Developer notes](#developer-notes)
  - [Testing](#testing)
  - [Migrating from version 1 to version 2](#migrating-from-version-1-to-version-2)
    - [Added to the Pusher class](#added-to-the-pusher-class)
    - [Changed in the Pusher class](#changed-in-the-pusher-class)
    - [Removed from the Pusher class](#removed-from-the-pusher-class)
    - [Removed from the Channel class](#removed-from-the-channel-class)
    - [Added to the GenericPresenceChannel class](#added-to-the-genericpresencechannel-class)
    - [Changed in the GenericPresenceChannel class](#changed-in-the-genericpresencechannel-class)
    - [Removed from the GenericPresenceChannel class](#removed-from-the-genericpresencechannel-class)
    - [Removed from the ConnectionState enum](#removed-from-the-connectionstate-enum)
    - [Added to the ErrorCodes enum](#added-to-the-errorcodes-enum)
- [License](#license)

## Installation

The compiled library is available on NuGet:

```
Install-Package PusherClient
```

## API

### Overview
Here's the API in a nutshell.

```cs

class ChatMember
{
    public string Name { get; set; }
}

class ChatMessage : ChatMember
{
    public string Message { get; set; }
}

// Raised when Pusher is ready
AutoResetEvent readyEvent = new AutoResetEvent(false);

// Raised when Pusher is done
AutoResetEvent doneEvent = new AutoResetEvent(false);

// Create Pusher client ready to subscribe to public, private and presence channels
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
    Encrypted = true,
});

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
    }
}

// MemberAdded event handler
void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has joined");
    if (sender is GenericPresenceChannel<ChatMember> channel)
    {
        ListMembers(channel);
    }
}

// MemberRemoved event handler
void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has left");
    if (sender is GenericPresenceChannel<ChatMember> channel)
    {
        ListMembers(channel);
    }
}

// Handle errors
void HandleError(object sender, PusherException error)
{
    if ((int)error.PusherCode < 5000)
    {
        // Error recevied from Pusher cluster, use PusherCode to filter.
    }
    else
    {
        if (error is ChannelUnauthorizedException unauthorizedAccess)
        {
            // Private and Presence channel failed authorization with Forbidden (403)
        }
        else if (error is ChannelAuthorizationFailureException httpError)
        {
            // Authorization endpoint returned an HTTP error other than Forbidden (403)
        }
        else if (error is OperationTimeoutException timeoutError)
        {
            // A client operation has timed-out. Governed by PusherOptions.ClientTimeout
        }
        else
        {
            // Handle other errors
        }
    }

    Trace.TraceError($"{error}");
}

// Subscribed event handler
void SubscribedHandler(object sender, Channel channel)
{
    if (channel is GenericPresenceChannel<ChatMember> presenceChannel)
    {
        ListMembers(presenceChannel);
    }
    else if (channel.Name == "private-chat-channel-1")
    {
        // Trigger event
        channel.Trigger("client-chat-event", new ChatMessage
        {
            Name = "Joe",
            Message = "Hello from Joe!",
        });
    }
}

// Connection state change event handler
void StateChangedEventHandler(object sender, ConnectionState state)
{
    Trace.TraceInformation($"SocketId: {((Pusher)sender).SocketID}, State: {state}");
    if (state == ConnectionState.Connected)
    {
        readyEvent.Set();
        readyEvent.Reset();
    }
    if (state == ConnectionState.Disconnected)
    {
        doneEvent.Set();
        doneEvent.Reset();
    }
}

// Bind events
void BindEvents(object sender)
{
    Pusher _pusher = sender as Pusher;
    Channel _channel = _pusher.GetChannel("private-chat-channel-1");
    _channel.Bind("client-chat-event", (PusherEvent eventData) =>
    {
        ChatMessage data = JsonConvert.DeserializeObject<ChatMessage>(eventData.Data);
        Trace.TraceInformation($"[{data.Name}] {data.Message}");
    });
}

// Unbind events
void UnbindEvents(object sender)
{
    ((Pusher)sender).UnbindAll();
}

// Add event handlers
pusher.Connected += BindEvents;
pusher.Disconnected += UnbindEvents;
pusher.Subscribed += SubscribedHandler;
pusher.ConnectionStateChanged += StateChangedEventHandler;
pusher.Error += HandleError;

// Create subscriptions
await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
GenericPresenceChannel<ChatMember> memberChannel =
    await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
memberChannel.MemberAdded += ChatMemberAdded;
memberChannel.MemberRemoved += ChatMemberRemoved;

// Connect
try
{
    await pusher.ConnectAsync().ConfigureAwait(false);
}
catch (Exception)
{
    // We failed to connect, handle the error.
    // You will also receive the error via
    // HandleError(object sender, PusherException error)
    throw;
}

Assert.AreEqual(ConnectionState.Connected, pusher.State);
Assert.IsTrue(readyEvent.WaitOne(TimeSpan.FromSeconds(5)));

// Remove subscriptions
await pusher.UnsubscribeAllAsync().ConfigureAwait(false);

// Disconnect
await pusher.DisconnectAsync().ConfigureAwait(false);
Assert.AreEqual(ConnectionState.Disconnected, pusher.State);
Assert.IsTrue(doneEvent.WaitOne(TimeSpan.FromSeconds(5)));

```
### Sample application

See [the example app](https://github.com/pusher/pusher-websocket-dotnet/tree/master/ExampleApplication) for details.

## Configuration

### The PusherOptions object

Constructing a Pusher client requires some options. These options are defined in a `PusherOptions` object:

| Property                    | Type              | Description                                                                                                                                   |
|-----------------------------|-------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| Authorizer                  | IAuthorizer       | Required for authorizing private and presence channel subscriptions.                                                                          |
| ClientTimeout               | TimeSpan          | The timeout period to wait for an asynchrounous operation to complete. The default value is 30 seconds.                                       |
| Cluster                     | String            | The Pusher host cluster name; for example, "eu". The default value is "mt1".                                                                  |
| Encrypted                   | Boolean           | Indicates whether the connection will be encrypted. The default value is `false`.                                                             |
| TraceLogger                 | ITraceLogger      | Used for tracing diagnostic events. Should not be set in production code.                                                                     |

### Application Key

The Pusher client constructor requires an application key which you can get from the app's API Access section in the Pusher Channels dashboard. It also takes a PusherOptions object as an input parameter.

Examples:

Construction of a public channel only subscriber

```cs

Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Cluster = Config.Cluster,
    Encrypted = true,
});

```

Construction of authorized private and presence channels subscriber

```cs

Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new HttpAuthorizer("http://localhost:8888/auth/Jane"),
    Cluster = Config.Cluster,
    Encrypted = true,
});

```

Specifying a client timeout

```cs

Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new HttpAuthorizer("http://localhost:8888/auth/Jane"),
    Cluster = Config.Cluster,
    Encrypted = true,
    ClientTimeout = TimeSpan.FromSeconds(20),
});

```
## Connecting

### Connection States
You can access the current connection state using `Pusher.State` and bind to state changes using `Pusher.ConnectionStateChanged`.

Possible States:
* Uninitialized: Initial state; no event is emitted in this state.
* Connecting: The channel client is attempting to connect.
* Connected: The channel connection is open and authenticated with your app. Channel subscriptions can now be made.
* Disconnecting: The channel client is attempting to disconnect.
* Disconnected: The channel connection is disconnected and all subscriptions have stopped.
* WaitingToReconnect: Once connected the channel client will automatically attempt to reconnect if there is a connection failure.

First call to `Pusher.ConnectAsync()` succeeds:
`Uninitialized -> Connecting -> Connected`. Auto reconnect is enabled.

First call to `Pusher.ConnectAsync()` fails:
`Uninitialized -> Connecting`. Auto reconnect is disabled.
`Pusher.ConnectAsync()` will throw an exception and a `Pusher.Error` will be emitted.

Call to `Pusher.DisconnectAsync()` succeeds:
`Connected -> Disconnecting -> Disconnected`. Auto reconnect is disabled.

Call to `Pusher.DisconnectAsync()` fails:
`Connected -> Disconnecting`.
`Pusher.DisconnectAsync()` will throw an exception.

Sample code:

```cs
pusher = new Pusher("YOUR_APP_KEY");
pusher.ConnectionStateChanged += StateChanged;
pusher.Error += ErrorHandler;

void StateChanged(object sender, ConnectionState state)
{
    Console.WriteLine("Connection state: " + state.ToString());
}

void ErrorHandler(object sender, PusherException error)
{
    Console.WriteLine("Pusher error: " + error.ToString());
}

try
{
    await pusher.ConnectAsync().ConfigureAwait(false);
    // pusher.State will be ConnectionState.Connected
}
catch(Exception error)
{
    // Handle error
}
```
### Auto reconnect
After entering a state of `Connected`, auto reconnect is enabled. If the connection is dropped the channel client will attempt to re-establish the connection.

 Here is a possible scenario played out after a connection failure.
* Connection dropped
* State transition: `Connected -> Disconnected`
* State transition: `Disconnected -> WaitingToReconnect`
* Pause for 1 second and attempt to reconnect
* State transition: `WaitingToReconnect -> Connecting`
* Connection attempt fails, network still down
* State transition: `Connecting -> Disconnected`
* State transition: `Disconnected -> WaitingToReconnect`
* Pause for 2 seconds (max 10 seconds) and attempt to reconnect
* State transition: `WaitingToReconnect -> Connecting`
* Connection attempt succeeds
* State transition: `Connecting -> Connected`

### Disconnecting

After disconnecting, all subscriptions are stopped and no events will be received from the Pusher server host. Calling ConnectAsync will re-enable any subscriptions that were stopped when disconnecting. To permanently stop receiving events, unsubscribe from the channel.

```cs

// Disconnect
await pusher.DisconnectAsync().ConfigureAwait(false);
Assert.AreEqual(ConnectionState.Disconnected, pusher.State);

```
### Connected and Disconnected delegates

The Pusher client has delegates for `Connected` and `Disconnected` events.

The `Connected` event is raised after connecting to the cluster host.

The `Disconnected` event is raised after disconnecting from the cluster host.

Sample code:

```cs
pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Authorizer = new HttpAuthorizer("YOUR_ENDPOINT")
});

pusher.Connected += OnConnected;
pusher.Disconnected += OnDisconnected;

void OnConnected(object sender)
{
    Console.WriteLine("Connected: " + ((Pusher)sender).SocketID);
}

void OnDisconnected(object sender)
{
    Console.WriteLine("Disconnected: " + ((Pusher)sender).SocketID);
}

try
{
    await pusher.ConnectAsync().ConfigureAwait(false);
    // pusher.State will now be ConnectionState.Connected
}
catch(Exception error)
{
    // Handle error
}

do
{
    line = Console.ReadLine();
    if (line == "quit") break;
} while (line != null);

// Disconnect
await pusher.DisconnectAsync().ConfigureAwait(false);
// pusher.State will now be ConnectionState.Disconnected

```

## Subscribing

Three types of channels are supported
* Public channels: can be subscribed to by anyone who knows their name.
* Private channels: are private in that you control access to who can subscribe to the channel.
* Presence channels: are extensions to private channels and let you add user information on a subscription, and let other members of the channel know who is online.

There are two modes for creating channel subscriptions
* Pre-connecting: setup all subscriptions first and then connect using `Pusher.ConnectAsync`. This is a purely asynchronous model. The subscriptions are created asynchronously after connecting. Error detection needs to be done via the error delegate `Pusher.Error`.
* Post-connecting: setup all subscriptions after connecting using `Pusher.ConnectAsync`. This is a partly synchronous model. The subscriptions are created synchronously. If the method call fails; for example, the user is not authorized, an exception will be thrown. You will also receive an error via the delegate `Pusher.Error`.

### Error handling

Regardless of the mode used, it is good to have error handling via the `Pusher.Error` delegate. For example, if you were to lose the network connection and the client attempts to auto reconnect, things happen asynchronously and the error handler is the only way to detect errors. Here is a sample error handler:

```cs

void ErrorHandler(object sender, PusherException error)
{
    if ((int)error.PusherCode < 5000)
    {
        // Error recevied from Pusher cluster, use PusherCode to filter.
    }
    else
    {
        if (error is ChannelUnauthorizedException unauthorizedAccess)
        {
            // Private and Presence channel failed authorization with Forbidden (403)
        }
        else if (error is ChannelAuthorizationFailureException httpError)
        {
            // Authorization endpoint returned an HTTP error other than Forbidden (403)
        }
        else if (error is OperationTimeoutException timeoutError)
        {
            // A client operation has timed-out. Governed by PusherOptions.ClientTimeout
        }
        else
        {
            // Handle other errors
        }
    }
}

```

ErrorHandler will be referred to in the channel subscription examples.

### Public channels

Setting up a public channel subscription before connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Subscribe
Channel channel = await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
Assert.AreEqual(false, channel.IsSubscribed);

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

```

Setting up a public channel subscription after connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

// Subscribe
try
{
    Channel channel = await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
    Assert.AreEqual(true, channel.IsSubscribed);
}
catch (Exception)
{
    // Handle error
}

```
### Private channels

The name of a private channel needs to start with `private-`.

It's possible to subscribe to [private channels](https://pusher.com/docs/channels/using_channels/private-channels) that provide a mechanism for [authenticating channel subscriptions](https://pusher.com/docs/channels/server_api/authenticating-users). In order to do this you need to provide an `IAuthorizer` when creating the `Pusher` instance.

The library provides an `HttpAuthorizer` implementation of `IAuthorizer` which makes an HTTP `POST` request to an authenticating endpoint. However, you can implement your own authentication mechanism if required.

Setting up a private channel subscription before connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Subscribe
Channel channel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
Assert.AreEqual(false, channel.IsSubscribed);

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

```

Setting up a private channel subscription after connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

try
{
    // Subscribe
    Channel channel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
    Assert.AreEqual(true, channel.IsSubscribed);
}
catch (ChannelUnauthorizedException)
{
    // Handle client unauthorized error
}
catch (Exception)
{
    // Handle other errors
}

```

### Presence channels

The name of a presence channel needs to start with `presence-`.

The recommended way of subscribing to a presence channel is to use the `SubscribePresenceAsync` function, as opposed to the standard subscribe function. This function returns a strongly typed presence channel object with extra presence channel specific functions available to it, such as `GetMember` and `GetMembers`.

Setting up a presence channel subscription before connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
    }
}

// MemberAdded event handler
void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has joined");
    ListMembers(sender as GenericPresenceChannel<ChatMember>);
}

// MemberRemoved event handler
void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has left");
    ListMembers(sender as GenericPresenceChannel<ChatMember>);
}

// Subscribe
GenericPresenceChannel<ChatMember> memberChannel =
    await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
memberChannel.MemberAdded += ChatMemberAdded;
memberChannel.MemberRemoved += ChatMemberRemoved;
Assert.AreEqual(false, memberChannel.IsSubscribed);

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

```

Setting up a presence channel subscription after connecting

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
    }
}

// MemberAdded event handler
void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has joined");
    ListMembers(sender as GenericPresenceChannel<ChatMember>);
}

// MemberRemoved event handler
void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has left");
    ListMembers(sender as GenericPresenceChannel<ChatMember>);
}

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

// Subscribe
try
{
    GenericPresenceChannel<ChatMember> memberChannel =
        await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
    memberChannel.MemberAdded += ChatMemberAdded;
    memberChannel.MemberRemoved += ChatMemberRemoved;
    Assert.AreEqual(true, memberChannel.IsSubscribed);
}
catch (ChannelUnauthorizedException)
{
    // Handle client unauthorized error
}
catch (Exception)
{
    // Handle other errors
}

```

### Subscribed delegate

The `Subscribed` delegate is invoked when a channel is subscribed to. There are two different ways to specify a Subscribed event handler:
* Add an event handler using the `Pusher.Subscribed` delegate property. This event handler can be used to detect all channel subscriptions.
* Provide an event handler as an input parameter to the `SubscribeAsync` or `SubscribePresenceAsync` methods. This event handler is channel specific.

Detect all channel subscribed events using `Pusher.Subscribed`

```cs

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
    }
}

// Subscribed event handler
void SubscribedHandler(object sender, Channel channel)
{
    if (channel is GenericPresenceChannel<ChatMember> presenceChannel)
    {
        ListMembers(presenceChannel);
    }
    else if (channel.Name == "private-chat-channel-1")
    {
        // Trigger event
        channel.Trigger("client-chat-event", new ChatMessage
        {
            Name = "Joe",
            Message = "Hello from Joe!",
        });
    }
}

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Add subcribed event handler
pusher.Subscribed += SubscribedHandler;

// Create subscriptions
await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

```

Detect a subscribed event on a channel

```cs

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
    }
}

// Presence channel subscribed event handler
void PresenceChannelSubscribed(object sender)
{
    ListMembers(sender as GenericPresenceChannel<ChatMember>);
}

// Private channel subscribed event handler
void PrivateChannelSubscribed(object sender)
{
    // Trigger event
    ((Channel)sender).Trigger("client-chat-event", new ChatMessage
    {
        Name = "Joe",
        Message = "Hello from Joe!",
    });
}

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Create subscriptions
await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
await pusher.SubscribeAsync("private-chat-channel-1", PrivateChannelSubscribed)
    .ConfigureAwait(false);
await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1", PresenceChannelSubscribed)
    .ConfigureAwait(false);

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

```
### Unsubscribe

You can use `UnsubscribeAsync` and `UnsubscribeAllAsync` to remove subscriptions.

```cs

// Remove a channel subscription
await pusher.UnsubscribeAsync("public-channel-1").ConfigureAwait(false);

// Remove all channel subscriptions
await pusher.UnsubscribeAllAsync().ConfigureAwait(false);

```

## Binding to events

Events can be bound to at two levels; per-channel or globally.

The binding samples make use of the following classes

```cs

class ChatMember
{
    public string Name { get; set; }
}

class ChatMessage : ChatMember
{
    public string Message { get; set; }
}

```

### Per-channel

These are bound to a specific channel, and mean that you can reuse event names in different parts of your client application.

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Channel event listener
void ChannelListener(PusherEvent eventData)
{
    ChatMessage data = JsonConvert.DeserializeObject<ChatMessage>(eventData.Data);
    Trace.TraceInformation($"Message from '{data.Name}': {data.Message}");
}

// Subscribe
Channel channel = await pusher.SubscribeAsync("private-chat-channel-1")
    .ConfigureAwait(false);

// Bind event listener to channel
channel.Bind("client-chat-event", ChannelListener);

// Unbind event listener from channel
channel.Unbind("client-chat-event", ChannelListener);

// Unbind all "client-chat-event" event listeners from the channel
channel.Unbind("client-chat-event");

// Unbind all event listeners from the channel
channel.UnbindAll();

```

### Globally

You can attach behavior to these events regardless of the channel the event is broadcast to.

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Global event listener
void GlobalListener(string eventName, PusherEvent eventData)
{
    if (eventName == "client-chat-event")
    {
        ChatMessage data = JsonConvert.DeserializeObject<ChatMessage>(eventData.Data);
        Trace.TraceInformation($"Message from '{data.Name}': {data.Message}");
    }
}

// Bind global event listener
pusher.BindAll(GlobalListener);

// Unbind global event listener
pusher.Unbind(GlobalListener);

// Unbind all event listeners associated with the Pusher client
pusher.UnbindAll();

```

## Triggering events

Once a [private](https://pusher.com/docs/channels/using_channels/private-channels) or [presence](https://pusher.com/docs/channels/using_channels/presence-channels) subscription has been authorized (see [authenticating users](https://pusher.com/docs/channels/server_api/authenticating-users)) and the subscription has succeeded, it is possible to trigger events on those channels.

```cs

// Create client
Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
{
    Authorizer = new FakeAuthoriser(),
    Cluster = Config.Cluster,
});
pusher.Error += ErrorHandler;

// Connect
await pusher.ConnectAsync().ConfigureAwait(false);

// Subscribe
Channel channel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);

// Trigger event
channel.Trigger("client-chat-event", new ChatMessage
{
    Name = "Joe",
    Message = "Hello from Joe!",
});

```

Events triggered by clients are called [client events](https://pusher.com/docs/channels/using_channels/events#triggering-client-events). Because they are being triggered from a client which may not be trusted there are a number of enforced rules when using them. Some of these rules include:

* Event names must have a `client-` prefix
* Rate limits
* You can only trigger an event when the subscription has succeeded

For full details see the [client events documentation](https://pusher.com/docs/channels/using_channels/events#triggering-client-events).

## Developer notes

The Pusher application settings are now loaded from a JSON config file stored in the root of the source tree and named `AppConfig.test.json`. Make a copy of `./AppConfig.sample.json` and name it `AppConfig.test.json`. Modify the contents of `AppConfig.test.json` with your test application settings. All tests should pass. The AuthHost and ExampleApplication should also run without any start-up errors.

### Testing

The majority of the tests are concurrency tests and the more the number of CPU(s) used the better. All tests should pass. However, some of the error code test paths fail intermittently when running on 2 CPU(s) - default build server configuration. With this in mind it is good to test on a 2 CPU configuration. A test settings file has been added to the root (CPU.count.2.runsettings) and you can specify it when running the tests via the menu option [Test]/[Configure Run Settings].

Also, a random latency is induced when authorizing a subscription. This is to weed out some of the concurrency issues. This adds to the time it takes to run all the tests. If you are running the tests often, you can speed things up by disabling the latency induction. Modify the property:

```cs
/// <summary>
/// Gets or sets whether this latency inducer is enabled.
/// </summary>
public bool Enabled { get; set; } = true;
```

Remember to set it back to `true` as it is enabled by default for a reason.


### Migrating from version 1 to version 2

You are encouraged to move to the Pusher Client SDK version 2. This major release includes a number of bug fixes and perfomance improvemnets. See the [changelog](https://github.com/pusher/pusher-websocket-dotnet/blob/master/CHANGELOG.md) for more information.

The following sections describe the changes in detail and describes how to update your code to the new version 2.x. If you run into problems you can always contact support@pusher.com.

#### Changed in the Pusher class

Previously the `ConnectAsync` returned a `ConnectionState` enum value after an async await. This is no longer the case; it returns void now. After the async await the state is always `Connected` if the call succeeds. If the call fails an exception will be thrown.

#### Removed from the Pusher class

Removed the public property `ConcurrentDictionary<string, Channel> Channels`. Use the method `GetAllChannels()` instead.

Removed the public static property `TraceSource Trace`. Use `PusherOptions.TraceLogger` instead.

#### Removed from the Channel class

Removed the public event delegate `SubscriptionEventHandler Subscribed`. Use the optional input parameter `SubscriptionEventHandler subscribedEventHandler` on `Pusher.SubscribeAsync` and `Pusher.SubscribePresenceAsync` instead. Alternatively, use `Pusher.Subscribed`.

#### Removed from the GenericPresenceChannel class

Removed the public property `ConcurrentDictionary<string, T> Members`. Use `GetMembers()` instead.

#### Removed from the ConnectionState enum

Six states remain simplifying the state change model:
* Uninitialized
* Connecting - Unimplemented state change in SDK version 1
* Connected - Unimplemented state change in SDK version 1
* Disconnecting
* Disconnected
* WaitingToReconnect

These states have been removed:
* Initialized
* NotConnected
* AlreadyConnected
* ConnectionFailed
* DisconnectionFailed

#### Changed in the GenericPresenceChannel class

The signature of the `MemberRemoved` delegate has changed from `MemberRemovedEventHandler MemberRemoved` to `MemberRemovedEventHandler<T> MemberRemoved`. This addresses issue #35.

#### Added to the Pusher class

An optional parameter `SubscriptionEventHandler subscribedEventHandler` has been added to the `SubscribeAsync` and `SubscribePresenceAsync<T>` methods.

```cs
/// <summary>
/// Fires when a channel becomes subscribed.
/// </summary>
public event SubscribedEventHandler Subscribed;
```

```cs
/// <summary>
/// Gets a channel.
/// </summary>
/// <param name="channelName">The name of the channel to get.</param>
/// <returns>The <see cref="Channel"/> if it exists; otherwise <c>null</c>.</returns>
public Channel GetChannel(string channelName)
{
    // ...
}
```

```cs
/// <summary>
/// Get all current channels.
/// </summary>
/// <returns>A list of the current channels.</returns>
public IList<Channel> GetAllChannels()
{
    // ...
}
```

```cs
/// <summary>
/// Removes a channel subscription.
/// </summary>
/// <param name="channelName">The name of the channel to unsubscribe.</param>
/// <returns>An awaitable task to use with async operations.</returns>
public async Task UnsubscribeAsync(string channelName)
{
    // ...
}
```

```cs
/// <summary>
/// Removes all channel subscriptions.
/// </summary>
/// <returns>An awaitable task to use with async operations.</returns>
public async Task UnsubscribeAllAsync()
{
    // ...
}
```


#### Added to the GenericPresenceChannel class

```cs
/// <summary>
/// Gets a member using the member's user ID.
/// </summary>
/// <param name="userId">The member's user ID.</param>
/// <returns>Retruns the member if found; otherwise returns null.</returns>
public T GetMember(string userId)
{
    // ...
}
```

```cs
/// <summary>
/// Gets the current list of members as a <see cref="Dictionary{TKey, TValue}"/>
/// where the TKey is the user ID and TValue is the member detail.
/// </summary>
/// <returns>Returns a <see cref="Dictionary{TKey, TValue}"/> containing the current members.</returns>
public Dictionary<string, T> GetMembers()
{
    // ...
}
```

#### Added to the ErrorCodes enum

Error codes less than 5000 remain the same. These are the error codes you can get from the Pusher server.

Error codes 5000 and above have been added for Client SDK detected errors. Almost all of these codes can be associated with a new exception class that derives from `PusherException`.

## License

This code is free to use under the terms of the MIT license.
