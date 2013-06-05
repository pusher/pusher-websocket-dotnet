%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe pusher-dotnet-client.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger

if exist Download\package rm -rf Download\package
if not exist Download\package\lib\net40 mkdir Download\package\lib\net40\

copy README.md Download\Package\

copy PusherClient\bin\Release\PusherClient.dll Download\Package\lib\net40\

copy PusherClient\bin\Release\PusherClient.xml Download\Package\lib\net40\

tools\nuget.exe update -self
tools\nuget.exe pack pusher-dotnet-client.nuspec -BasePath Download\Package -Output Download