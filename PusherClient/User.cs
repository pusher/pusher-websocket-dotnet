using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PusherClient
{
     public class User : IUser
     {
          private static class UserConnectionStateChangedEventHandler: ConnectionStateChangedEventHandler {
               private User user;

               public UserConnectionStateChangedEventHandler(User user) {
                    this.user = user;
               }

               public override void ChangeConnectionState(ConnectionState state){
                    switch (connection._pusher.State) {
                         case ConnectionState.Connected:
                              user.attemptSignin();
                              break;
                         case ConnectionState.Connecting:
                              user.disconnect();
                              break;
                         case ConnectionState.Disconnected:
                              user.disconnect();
                              break;
                    }
               }
          }

          private IConnection connection;
          private IUserAuthenticator userAuthenticator;
          private boolean signinRequested;
          private Channel serverToUserChannel;
          private String userId;

          public User(IConnection connection, IUserAuthenticator userAuthenticator) {
               this.connection = connection;
               this.userAuthenticator = userAuthenticator;
               this.signinRequested = false;

               pusher.BindAll(GeneralListener);
          }


          void GeneralListener(string eventName, PusherEvent eventData)
          {
               if (eventName == Constants.PUSHER_SIGNIN_SUCCESS)
               {
                    onSigninSuccess(event);
               }
          }

          public void signin() {
               if (signinRequested || userId != null) {
                    return;
               }
                    signinRequested = true;
               try 
               {        
                    attemptSignin();
               }  
               catch (Exception e)
               {
                    throw new UserAuthenticationFailureException(e.Message, ErrorCodes.UserAuthenticationError);
               }

          }

          private void attemptSignin(){
               if (!signinRequested || userId != null) {
                    return;
               }

               if (pusher.State != ConnectionState.Connected) {
                    // Signin will be attempted when the connection is connected
                    return;
               }

               AuthenticationResponse authenticationResponse = getAuthenticationResponse();
               connection.SendAsync(authenticationResponseToSigninMessage(authenticationResponse));
          }

          private static String authenticationResponseToSigninMessage(AuthenticationResponse authenticationResponse) {
               return DefaultSerializer.Default.Serialize(new SigninMessage(authenticationResponse.getAuth(), authenticationResponse.getUserData()));
          }

          private AuthenticationResponse getAuthenticationResponse() throws AuthenticationFailureException {
               String response = userAuthenticator.Authenticate(connection.SocketId);
               try {
                    AuthenticationResponse authenticationResponse = Serialize(response, AuthenticationResponse.class);
                    if (authenticationResponse.getAuth() == null || authenticationResponse.getUserData() == null) {
                         throw new UserAuthenticationFailureException(
                              "Didn't receive all the fields expected from the UserAuthenticator. Expected auth and user_data", null
                         );
                    }
                    return authenticationResponse;
               } catch (PusherException e) {
                    throw new UserAuthenticationFailureException("Unable to parse response from AuthenticationResponse", e);
               }
          }

          private void onSigninSuccess(PusherEvent event) {
               try {
                    JToken jToken = JToken.Parse(event);
                    JObject jObject = JObject.Parse(jToken.ToString());
                    userData = jObject.SelectToken("user_data");
                    id = jObject.SelectToken("id");
                    if (id.Type == JTokenType.String)
                     {
                          userId = id.Value<string>();
                     }

               } catch (Exception error) {
                    errorMsg = "Failed parsing user data after signin"
                    throw new PusherException(errorMsg, error)
               }

               if (userId == null) {
                    return;
               }
               this.serverToUserChannel = new Channel($"#server-to-user-'{userId}'", connection._pusher)
               connection._pusher.Subscribe($"#server-to-user-'{userId}'", null);
          }

          private void disconnect() {
               if (serverToUserChannel.IsSubscribed) {
                    serverToUserChannel.Unsubscribe();
               }
               userId = null;
          }

          public override String UserID() {
               return userId;
          }

          public override void Bind(String eventName, IEventBinder<TData> listener) {
               serverToUserChannel.bind(eventName, listener);
          }

          public override void BindAll(IEventBinder<TData> listener) {
               serverToUserChannel.bindGlobal(listener);
          }

          public override void Unbind(String eventName, IEventBinder<TData> listener) {
               serverToUserChannel.unbind(eventName, listener);
          }

          public override void UnbindAll(IEventBinder<TData> listener) {
               serverToUserChannel.unbindGlobal(listener);
          }

     }
}