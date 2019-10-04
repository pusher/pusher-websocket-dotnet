"%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\msbuild.exe" pusher-dotnet-client.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger

"%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\msbuild.exe" PusherClient/PusherClient.csproj /t:pack /p:configuration=release