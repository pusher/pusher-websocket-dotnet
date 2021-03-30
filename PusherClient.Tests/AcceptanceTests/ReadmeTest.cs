using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Validates code snippets used in README.md.
    /// </summary>
    [TestFixture]
    public class ReadmeTest
    {
        #region Overview classes

        class ChatMember
        {
            public string Name { get; set; }
        }

        class ChatMessage : ChatMember
        {
            public string Message { get; set; }
        }

        #endregion Overview classes

        /// <summary>
        /// Sample used in the Readme file.
        /// </summary>
        /// <remarks>
        /// Run AuthHost.exe to get this test to work without errors reported to HandleError.
        /// </remarks>
        /// <returns></returns>
        [Test]
        public async Task ApiOverviewAsync()
        {
            #region API Overview

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
                    Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
                }
            }

            // MemberAdded event handler
            void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has joined");
                if (sender is GenericPresenceChannel<ChatMember> channel)
                {
                    ListMembers(channel);
                }
            }

            // MemberRemoved event handler
            void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has left");
                if (sender is GenericPresenceChannel<ChatMember> channel)
                {
                    ListMembers(channel);
                }
            }

            // Handle errors
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

                Trace.TraceError($"{error}");
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
                Trace.TraceInformation($"SocketId: {((Pusher)sender).SocketID}, State: {state}");
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
            await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
            await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
            GenericPresenceChannel<ChatMember> memberChannel =
                await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
            memberChannel.MemberAdded += ChatMemberAdded;
            memberChannel.MemberRemoved += ChatMemberRemoved;

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

            #endregion API Overview
        }

        [Test]
        public async Task ConstructionOfAPublicChannelOnlySubscriberAsync()
        {
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Cluster = Config.Cluster,
                Encrypted = true,
            });

            await pusher.ConnectAsync().ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructionOfAnAuthorizedChannelSubscriberAsync()
        {
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new HttpAuthorizer("http://localhost:8888/auth/Jane"),
                Cluster = Config.Cluster,
                Encrypted = true,
            });

            await pusher.ConnectAsync().ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SpecifyingAClientTimeoutAsync()
        {
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new HttpAuthorizer("http://localhost:8888/auth/Jane"),
                Cluster = Config.Cluster,
                Encrypted = true,
                ClientTimeout = TimeSpan.FromSeconds(20),
            });

            await pusher.ConnectAsync().ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectedAndDisconnectedDelegatesAsync()
        {
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Cluster = Config.Cluster,
                Encrypted = true,
            });

            void OnConnected(object sender)
            {
                Console.WriteLine("Connected: " + ((Pusher)sender).SocketID);
            }

            void OnDisconnected(object sender)
            {
                Console.WriteLine("Disconnected: " + ((Pusher)sender).SocketID);
            }

            pusher.Connected += OnConnected;
            pusher.Disconnected += OnDisconnected;

            await pusher.ConnectAsync().ConfigureAwait(false);
            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPublicChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Subscribe
            Channel publicChannel = await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
            Assert.AreEqual(false, publicChannel.IsSubscribed);

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePublicChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Subscribe
            try
            {
                Channel publicChannel = await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
                Assert.AreEqual(true, publicChannel.IsSubscribed);
            }
            catch (Exception)
            {
                // Handle error
            }

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPrivateChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Subscribe
            Channel publicChannel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
            Assert.AreEqual(false, publicChannel.IsSubscribed);

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePrivateChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            try
            {
                // Subscribe
                Channel publicChannel = await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
                Assert.AreEqual(true, publicChannel.IsSubscribed);
            }
            catch (ChannelUnauthorizedException)
            {
                // Handle client unauthorized error
            }
            catch (Exception)
            {
                // Handle other errors
            }

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task SubscribeThenConnectPresenceChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Lists all current peresence channel members
            void ListMembers(GenericPresenceChannel<ChatMember> channel)
            {
                Dictionary<string, ChatMember> members = channel.GetMembers();
                foreach (var member in members)
                {
                    Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
                }
            }

            // MemberAdded event handler
            void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has joined");
                ListMembers(sender as GenericPresenceChannel<ChatMember>);
            }

            // MemberRemoved event handler
            void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has left");
                ListMembers(sender as GenericPresenceChannel<ChatMember>);
            }

            // Subscribe
            GenericPresenceChannel<ChatMember> memberChannel =
                await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
            memberChannel.MemberAdded += ChatMemberAdded;
            memberChannel.MemberRemoved += ChatMemberRemoved;
            Assert.AreEqual(false, memberChannel.IsSubscribed);

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ConnectThenSubscribePresenceChannelAsync()
        {
            #region Code snippet

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Lists all current peresence channel members
            void ListMembers(GenericPresenceChannel<ChatMember> channel)
            {
                Dictionary<string, ChatMember> members = channel.GetMembers();
                foreach (var member in members)
                {
                    Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
                }
            }

            // MemberAdded event handler
            void ChatMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has joined");
                ListMembers(sender as GenericPresenceChannel<ChatMember>);
            }

            // MemberRemoved event handler
            void ChatMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
            {
                Trace.TraceInformation($"Member {member.Value.Name} has left");
                ListMembers(sender as GenericPresenceChannel<ChatMember>);
            }

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            // Subscribe
            try
            {
                GenericPresenceChannel<ChatMember> memberChannel =
                    await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);
                memberChannel.MemberAdded += ChatMemberAdded;
                memberChannel.MemberRemoved += ChatMemberRemoved;
                Assert.AreEqual(true, memberChannel.IsSubscribed);
            }
            catch (ChannelUnauthorizedException)
            {
                // Handle client unauthorized error
            }
            catch (Exception)
            {
                // Handle other errors
            }

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task PusherSubscribedDelegateAsync()
        {
            #region Code snippet

            // Lists all current peresence channel members
            void ListMembers(GenericPresenceChannel<ChatMember> channel)
            {
                Dictionary<string, ChatMember> members = channel.GetMembers();
                foreach (var member in members)
                {
                    Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
                }
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

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Add subcribed event handler
            pusher.Subscribed += SubscribedHandler;

            // Create subscriptions
            await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
            await pusher.SubscribeAsync("private-chat-channel-1").ConfigureAwait(false);
            await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1").ConfigureAwait(false);

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task ChannelSubscribedDelegateAsync()
        {
            #region Code snippet

            // Lists all current peresence channel members
            void ListMembers(GenericPresenceChannel<ChatMember> channel)
            {
                Dictionary<string, ChatMember> members = channel.GetMembers();
                foreach (var member in members)
                {
                    Trace.TraceInformation($"Id: {member.Key}, Name: {member.Value.Name}");
                }
            }

            // Presence channel subscribed event handler
            void PresenceChannelSubscribed(object sender)
            {
                ListMembers(sender as GenericPresenceChannel<ChatMember>);
            }

            // Private channel subscribed event handler
            void PrivateChannelSubscribed(object sender)
            {
                // Trigger event
                ((Channel)sender).Trigger("client-chat-event", new ChatMessage { Name = "Joe", Message = "Hello from Joe!" });
            }

            // Create client
            Pusher pusher = new Pusher(Config.AppKey, new PusherOptions
            {
                Authorizer = new FakeAuthoriser(),
                Cluster = Config.Cluster,
            });
            pusher.Error += ErrorHandler;

            // Create subscriptions
            await pusher.SubscribeAsync("public-channel-1").ConfigureAwait(false);
            await pusher.SubscribeAsync("private-chat-channel-1", PrivateChannelSubscribed).ConfigureAwait(false);
            await pusher.SubscribePresenceAsync<ChatMember>("presence-channel-1", PresenceChannelSubscribed).ConfigureAwait(false);

            // Connect
            await pusher.ConnectAsync().ConfigureAwait(false);

            #endregion Code snippet

            await pusher.DisconnectAsync().ConfigureAwait(false);
        }

        private static void ErrorHandler(object sender, PusherException error)
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
        }
    }
}