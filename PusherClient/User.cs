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
          /*
          private static class UserConnectionStateChangedEventHandler: ConnectionStateChangedEventHandler {
               private User user;

               public UserConnectionStateChangedEventHandler(User user) {
                    this.user = user;
               }

               public override void ChangeConnectionState(ConnectionState state){
                    switch (connection._pusher.State) {
                         case ConnectionState.Connected:
                              user.AttemptSignin();
                              break;
                         case ConnectionState.Connecting:
                              user.Disconnect();
                              break;
                         case ConnectionState.Disconnected:
                              user.Disconnect();
                              break;
                    }
               }
          }
          */

          private IConnection connection;
          private IUserAuthenticator userAuthenticator;
          private bool signinRequested;
          private Channel serverToUserChannel;
          private String userId;

          public User(IConnection connection, IUserAuthenticator userAuthenticator) {
               this.connection = connection;
               this.userAuthenticator = userAuthenticator;
               this.signinRequested = false;

               pusher.BindAll(GeneralListener);
          }


          void GeneralListener(string eventName, PusherEvent e)
          {
               if (eventName == Constants.PUSHER_SIGNIN_SUCCESS)
               {
                    OnSigninSuccess(e);
               }
          }

          public void Signin() {
               if (signinRequested || userId != null) {
                    return;
               }
                    signinRequested = true;
               try 
               {        
                    AttemptSignin();
               }  
               catch (Exception e)
               {
                    throw new UserAuthenticationFailureException(e.Message, ErrorCodes.UserAuthenticationError);
               }

          }

          private void AttemptSignin(){
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

          private static String AuthenticationResponseToSigninMessage(AuthenticationResponse authenticationResponse) {
               return DefaultSerializer.Default.Serialize(new SigninMessage(authenticationResponse.getAuth(), authenticationResponse.getUserData()));
          }

          private AuthenticationResponse GetAuthenticationResponse() throws AuthenticationFailureException {
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

          private void OnSigninSuccess(PusherEvent e) {
               String userId = null;
               try {
                    JToken jToken = JToken.Parse(e);
                    JObject jObject = JObject.Parse(jToken.ToString());
                    JToken userData = jObject.SelectToken("user_data");
                    JToken id = jObject.SelectToken("id");
                    if (id.Type == JTokenType.String)
                     {
                          userId = id.Value<string>();
                     }

               } catch (Exception error) {
                    String errorMsg = "Failed parsing user data after signin";
                    throw new PusherException(errorMsg, error);
               }

               if (userId == null) {
                    return;
               }
               this.serverToUserChannel = new Channel($"#server-to-user-'{userId}'", connection._pusher)
               connection._pusher.Subscribe($"#server-to-user-'{userId}'", null);
          }

          private void Disconnect() {
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