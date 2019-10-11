# Pusher Channels .NET Client library

This is a .NET library for interacting with the Channels WebSocket API.

Register at [pusher.com/channels](https://pusher.com/channels) and use the application credentials within your app as shown below.

More general documentation can be found on the [Official Channels Documentation](https://pusher.com/docs/channels).

For integrating **Pusher Channels** with **Unity** follow the instructions at <https://github.com/pusher/pusher-websocket-unity>

## Supported platforms

* .NET Standard 1.6
* Unity 2018 and greater via [pusher-websocket-unity](https://github.com/pusher/pusher-websocket-unity)

## Installation

### NuGet Package

```
Install-Package PusherClient
```

## Usage

See [the example app](https://github.com/pusher/pusher-websocket-dotnet/tree/master/ExampleApplication) for full details.

### Connect

#### Event based
```cs
_pusher = new Pusher("YOUR_APP_KEY");
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
_pusher.Connect();
```

where `_pusher_ConnectionStateChanged` and `_pusher_Error` are custom event handlers such as

```cs
static void _pusher_ConnectionStateChanged(object sender, ConnectionState state)
{
    Console.WriteLine("Connection state: " + state.ToString());
}

static void _pusher_Error(object sender, PusherException error)
{
    Console.WriteLine("Pusher Channels Error: " + error.ToString());
}
```

#### Asynchronous
```cs
_pusher = new Pusher("YOUR_APP_KEY");
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
ConnectionState connectionState = await _pusher.ConnectAsync();
```

In the case of the async version, state and error changes will continue to be published via events, but the initial connection state will be returned from the `ConnectAsync()` method.

### Authenticated Connect
If you have an authentication endpoint for private or presence channels:

#### Event based
```cs
_pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Authorizer = new HttpAuthorizer("YOUR_ENDPOINT")
});
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
_pusher.Connect();
```

#### Asynchronous

```cs
_pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Authorizer = new HttpAuthorizer("YOUR_ENDPOINT")
});
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
ConnectionState connectionState = await _pusher.ConnectAsync();
```

### Non Default Cluster
If you are on a non default cluster (e.g. eu):

#### Event based
```cs
_pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Cluster = "eu"
});
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
_pusher.Connect();
```

#### Asynchonous
```cs
_pusher = new Pusher("YOUR_APP_KEY", new PusherOptions(){
    Cluster = "eu"
});
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;
ConnectionState connectionState = _pusher.ConnectAsync();
```

### Subscribe to a public or private channel

#### Event based
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

#### Asynchronous
```cs
Channel _myChannel = await _pusher.SubscribeAsync("my-channel");
```

### Bind to an event

```cs
_myChannel.Bind("my-event", (dynamic data) =>
{
    Console.WriteLine(data.message);
});
```

### Subscribe to a presence channel

#### Event based
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

#### Asynchronous

```cs
_presenceChannel = await (PresenceChannel)_pusher.SubscribeAsync("presence-channel");
_presenceChannel.Subscribed += _presenceChannel_Subscribed;
_presenceChannel.MemberAdded += _presenceChannel_MemberAdded;
_presenceChannel.MemberRemoved += _presenceChannel_MemberRemoved;
```

### Unbind

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

### Publish to NuGet

You should be familiar with [creating an publishing NuGet packages](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package).

From the `pusher-dotnet-client` directory:

1. Update `PusherClient/PusherClient.csproj` and  `PusherClient/Pusher.cs` with new version number etc.
2. Specify the correct path to `msbuild.exe` in `package.cmd` and run it.
3. Run `nuget push PusherClient.{VERSION}.nupkg`

## License

This code is free to use under the terms of the MIT license.
