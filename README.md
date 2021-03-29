# Pusher Channels .NET Client library

This is a .NET library for interacting with the Channels WebSocket API.

Register at [pusher.com/channels](https://pusher.com/channels) and use the application credentials within your app as shown below.

More general documentation can be found on the [Official Channels Documentation](https://pusher.com/docs/channels).

For integrating **Pusher Channels** with **Unity** follow the instructions at <https://github.com/pusher/pusher-websocket-unity>

## Supported platforms

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
	- [Connected and Disconnected delegates](#connected-and-disconnected-delegates)


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
    Authorizer = new FakeAuthoriser("Jane"),
    Cluster = Config.Cluster,
    Encrypted = true,
});

// Lists all current peresence channel members
void ListMembers(GenericPresenceChannel<ChatMember> channel)
{
    Dictionary<string, ChatMember> members = channel.GetMembers();
    foreach (var member in members)
    {
        Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}\n");
    }
}

// MemberAdded event handler
void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has joined\n");
    if (sender is GenericPresenceChannel<ChatMember> channel)
    {
        ListMembers(channel);
    }
}

// MemberRemoved event handler
void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
{
    Trace.TraceInformation($"Member {member.Value.Name} has left\n");
    if (sender is GenericPresenceChannel<ChatMember> channel)
    {
        ListMembers(channel);
    }
}

// Handles and records errors
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

    Trace.TraceError($"{error}\n");
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
        channel.Trigger("client-chat-event", new ChatMessage { Name = "Joe", Message = "Hello from Joe!" });
    }
}

// Connection state change event handler
void StateChangedEventHandler(object sender, ConnectionState state)
{
    Trace.TraceInformation($"SocketId: {((Pusher)sender).SocketID}, State: {state}\n");
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
await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false); ;
Channel chatChannel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false); ;
GenericPresenceChannel<ChatMember> presenceCh =
    await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false); ;
presenceCh.MemberAdded += ChatMemberAdded;
presenceCh.MemberRemoved += ChatMemberRemoved;

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

Construction of an authorized channel subscriber

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
void StateChanged(object sender, ConnectionState state)
{
    Console.WriteLine("Connection state: " + state.ToString());
}

void ErrorHandler(object sender, PusherException error)
{
    Console.WriteLine("Pusher error: " + error.ToString());
}

pusher = new Pusher("YOUR_APP_KEY");
pusher.ConnectionStateChanged += StateChanged;
pusher.Error += ErrorHandler;

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

### Connected and Disconnected delegates

The Pusher client has delegates for `Connected` and `Disconnected` events.

The `Connected` event is raised after connecting to the cluster host.

The `Disconnected` event is raised after disconnecting from the cluster host.

Sample code:

```cs
void OnConnected(object sender)
{
    Console.WriteLine("Connected: " + ((Pusher)sender).SocketID);
}

void OnDisconnected(object sender)
{
    Console.WriteLine("Disconnected: " + ((Pusher)sender).SocketID);
}

pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Authorizer = new HttpAuthorizer("YOUR_ENDPOINT")
});

pusher.Connected += OnConnected;
pusher.Disconnected += OnDisconnected;

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

## Subscribe to a public or private channel

### Event based
```cs
_myChannel = _pusher.Subscribe("my-channel");
_myChannel.Subscribed += _myChannel_Subscribed;
```
where `_myChannel_Subscribed` is a custom event handler such as

```cs
static void _myChannel_Subscribed(object sender)
{
    Console.WriteLine("Subscribed!");
}
```

### Asynchronous
```cs
Channel _myChannel = await _pusher.SubscribeAsync("my-channel");
```

## Bind to an event

```cs
_myChannel.Bind("my-event", (dynamic data) =>
{
    Console.WriteLine(data.message);
});
```

## Subscribe to a presence channel

### Event based
```cs
_presenceChannel = (PresenceChannel)_pusher.Subscribe("presence-channel");
_presenceChannel.Subscribed += _presenceChannel_Subscribed;
_presenceChannel.MemberAdded += _presenceChannel_MemberAdded;
_presenceChannel.MemberRemoved += _presenceChannel_MemberRemoved;
```

Where `_presenceChannel_Subscribed`, `_presenceChannel_MemberAdded`, and `_presenceChannel_MemberRemoved` are custom event handlers such as

```cs
static void _presenceChannel_MemberAdded(object sender, KeyValuePair<string, dynamic> member)
{
    Console.WriteLine((string)member.Value.name.Value + " has joined");
    ListMembers();
}

static void _presenceChannel_MemberRemoved(object sender)
{
    ListMembers();
}
```

### Asynchronous

```cs
_presenceChannel = await (PresenceChannel)_pusher.SubscribeAsync("presence-channel");
_presenceChannel.Subscribed += _presenceChannel_Subscribed;
_presenceChannel.MemberAdded += _presenceChannel_MemberAdded;
_presenceChannel.MemberRemoved += _presenceChannel_MemberRemoved;
```

## Unbind

Remove a specific callback:

```cs
_myChannel.Unbind("my-event", callback);
```

Remove all callbacks for a specific event:

```cs
_myChannel.Unbind("my-event");
```

Remove all bindings on the channel:

```cs
_myChannel.UnbindAll();
```

## Developer Notes

The Pusher application settings are now loaded from a JSON config file stored in the root of the source tree and named `AppConfig.test.json`. Make a copy of `./AppConfig.sample.json` and name it `AppConfig.test.json`. Modify the contents of `AppConfig.test.json` with your test application settings. All tests should pass. The AuthHost and ExampleApplication should also run without any start-up errors.

### Publish to NuGet

You should be familiar with [creating an publishing NuGet packages](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package).

From the `pusher-dotnet-client` directory:

1. Update `PusherClient/PusherClient.csproj` and  `PusherClient/Pusher.cs` with new version number etc.
2. Specify the correct path to `msbuild.exe` in `package.cmd` and run it.
3. Run `nuget push PusherClient.{VERSION}.nupkg`

## License

This code is free to use under the terms of the MIT license.
