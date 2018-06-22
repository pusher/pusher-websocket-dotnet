"%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" pusher-dotnet-client.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger

tools\nuget.exe update -self
tools\nuget.exe pack PusherClient\PusherClient.csproj -verbosity detailed -properties Configuration=Release