# pusher websocket dotnet Example application

# Run it on a mac

First thing is to [install dotnet](https://learn.microsoft.com/en-us/dotnet/core/install/macos).

Then you need to run a little server to implement authentication. Something [like this](https://gist.github.com/marcelcorso/5b1abded056a53935bb118cd5edc0d2f).

I ran it like this

```
export PUSHER_APP_ID=666
export PUSHER_KEY=bbb
export PUSHER_SECRET=aaa
export PUSHER_CLUSTER=mt1
export PORT=9000

npm start 
```

Then let's run the csharp stuff. 

Create a config. Maybe copy from an example

```
cp ../AppConfig.sample.json ../AppConfig.test.json
```

and then update it's values to match your pusher app. Same as the ones you are running the auth server with 

then run the example

```
dotnet run --framework net45
```


