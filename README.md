# Pusher .NET Client library

This is a .NET library for interacting with the Pusher WebSocket API.

Registering at <http://pusher.com> and use the application credentials within your app as shown below.

More general documentation can be found at <http://pusher.com/docs/>.

## Installation

### NuGet Package
```
Install-Package PusherClient -Pre
```

## Publish to NuGet

You should be familiar with [creating an publishing NuGet packages](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package).

From the `pusher-dotnet-client` directory:

1. Update `pusher-dotnet-client.nuspec` with new version number etc.
2. Run `package.cmd`
3. Run `tools/nuget.exe push Download/package/PusherClient.{VERSION}.nupkg'

## License

This code is free to use under the terms of the MIT license.