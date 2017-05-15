using System;
using System.Threading;
using NUnit.Framework;
using PusherClient.Tests.Utilities;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class Messages
    {
        [Test]
        public void PusherShouldSendAMessageWhenRequested()
        {
            // Arrange
            var pusher = PusherFactory.GetPusher(new PusherOptions { Authorizer = new FakeAuthoriser(UserNameFactory.CreateUniqueUserName()) });
            var connected = new AutoResetEvent(false);
            var subscribed1 = new AutoResetEvent(false);
            var eventReceived = new AutoResetEvent(false);

            pusher.Connected += sender =>
            {
                connected.Set();
            };

            pusher.Connect();
            connected.WaitOne(TimeSpan.FromSeconds(5));

            var mockChannelName = ChannelNameFactory.CreateUniqueChannelName(privateChannel: true);

            var channel = pusher.Subscribe(mockChannelName);
            channel.Subscribed += sender =>
            {
                subscribed1.Set();
            };

            dynamic receivedThing = null;
            string receivedString = null;

            channel.BindAll((s, o) =>
            {
                receivedString = s;
                receivedThing = o;
                eventReceived.Set();
            });

            subscribed1.WaitOne(TimeSpan.FromSeconds(5));

            // Act
            channel.Trigger("client-my-test-event", new { message = "This is a test", name = "test message" });

            eventReceived.WaitOne(TimeSpan.FromSeconds(30));

            // Assert
            StringAssert.Contains("client-my-test-event", receivedString);
            StringAssert.Contains("This is a test", receivedThing.message);
            StringAssert.Contains("test message", receivedThing.name);
        }
    }
}
