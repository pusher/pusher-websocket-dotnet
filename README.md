# Pusher .NET Client library

This is a .NET library for interacting with the Pusher WebSocket API.

Registering at <http://pusher.com> and use the application credentials within your app as shown below.

More general documentation can be found at <http://pusher.com/docs/>.

## Installation

### NuGet Package

```
Install-Package PusherClient
```

## Usage

```cs
_pusher = new Pusher("7899dd5cb232af88083d", new PusherOptions(){
    Authorizer = new HttpAuthorizer("http://localhost:8888/auth/" + HttpUtility.UrlEncode(_name))
});
_pusher.ConnectionStateChanged += _pusher_ConnectionStateChanged;
_pusher.Error += _pusher_Error;

// Setup private channel
_chatChannel = _pusher.Subscribe("private-channel");
_chatChannel.Subscribed += _chatChannel_Subscribed;

// Inline binding!
_chatChannel.Bind("client-my-event", (dynamic data) =>
{
    Console.WriteLine("[" + data.name + "] " + data.message);
});

// Setup presence channel
_presenceChannel = (PresenceChannel)_pusher.Subscribe("presence-channel");
_presenceChannel.Subscribed += _presenceChannel_Subscribed;
_presenceChannel.MemberAdded += _presenceChannel_MemberAdded;
_presenceChannel.MemberRemoved += _presenceChannel_MemberRemoved;
```

See [the example app](https://github.com/pusher-community/pusher-websocket-dotnet/tree/master/ExampleApplication) for full details.


## Developer Notes

### Publish to NuGet

You should be familiar with [creating an publishing NuGet packages](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package).

From the `pusher-dotnet-client` directory:

1. Update `pusher-dotnet-client.nuspec` with new version number etc.
2. Run `package.cmd`
3. Run `tools/nuget.exe push PusherClient.{VERSION}.nupkg`

## License

This code is free to use under the terms of the MIT license.