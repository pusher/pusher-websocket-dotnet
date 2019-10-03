using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PusherClient.Tests.AcceptanceTests
{
    [TestFixture]
    public class EventEmitter
    {
        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredListener()
        {
            // Arrange
            dynamic emittedEvent = null;

            var myAction = new Action<dynamic>(o => emittedEvent = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("listener event", myAction);

            // Act
            emitter.EmitEvent("listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("channel event", emittedEvent.@event.Value);
        }

        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredRawListener()
        {
            // Arrange
            string emittedEvent = null;

            var myAction = new Action<string>(o => emittedEvent = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("raw listener event", myAction);

            // Act
            emitter.EmitEvent("raw listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("{\"user_id\":\"a user\",\"channel\":\"a channel name\",\"event\":\"channel event\",\"data\":\"{\\\"stuff\\\":1234}\"}", emittedEvent);
        }

        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredPusherEventListener()
        {
            // Arrange
            PusherEvent emittedEvent = null;

            var myAction = new Action<PusherEvent>(o => emittedEvent = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("pusher event listener event", myAction);

            // Act
            emitter.EmitEvent("pusher event listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("channel event", emittedEvent.EventName);
            StringAssert.AreEqualIgnoringCase("\"{\\\"stuff\\\":1234}\"", emittedEvent.Data);
        }

        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredGeneralListener()
        {
            // Arrange
            Tuple<string, dynamic> emittedEvent = null;

            var myAction = new Action<string, dynamic>((e, o) => emittedEvent = new Tuple<string, dynamic>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);

            // Act
            emitter.EmitEvent("general listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("general listener event", emittedEvent.Item1);
            StringAssert.AreEqualIgnoringCase("channel event", emittedEvent.Item2.@event.Value);
        }

        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredRawGeneralListener()
        {
            // Arrange
            Tuple<string, string> emittedEvent = null;

            var myAction = new Action<string, string>((e, o) => emittedEvent = new Tuple<string, string>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);

            // Act
            emitter.EmitEvent("raw general listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("raw general listener event", emittedEvent.Item1);
            StringAssert.AreEqualIgnoringCase("{\"user_id\":\"a user\",\"channel\":\"a channel name\",\"event\":\"channel event\",\"data\":\"{\\\"stuff\\\":1234}\"}", emittedEvent.Item2);
        }

        [Test]
        public void EventEmitterShouldEmitAnEventToARegisteredPusherEventGeneralListener()
        {
            // Arrange
            Tuple<string, PusherEvent> emittedEvent = null;

            var myAction = new Action<string, PusherEvent>((e, o) => emittedEvent = new Tuple<string, PusherEvent>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);

            // Act
            emitter.EmitEvent("pusher event general listener event", CreateTestEvent());

            // Assert
            Assert.IsNotNull(emittedEvent);
            StringAssert.AreEqualIgnoringCase("pusher event general listener event", emittedEvent.Item1);
            StringAssert.AreEqualIgnoringCase("channel event", emittedEvent.Item2.EventName);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredListener()
        {
            // Arrange
            dynamic emittedEvent = null;
            dynamic emittedEvent2 = null;

            var myAction = new Action<dynamic>(o => emittedEvent = o);
            var myAction2 = new Action<dynamic>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("listener event", myAction);
            emitter.Bind("listener event", myAction2);
            emitter.Unbind("listener event", myAction);

            // Act
            emitter.EmitEvent("listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNotNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredRawListener()
        {
            // Arrange
            string emittedEvent = null;
            string emittedEvent2 = null;

            var myAction = new Action<string>(o => emittedEvent = o);
            var myAction2 = new Action<string>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("raw listener event", myAction);
            emitter.Bind("raw listener event", myAction2);
            emitter.Unbind("raw listener event", myAction);

            // Act
            emitter.EmitEvent("raw listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNotNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredPusherEventListener()
        {
            // Arrange
            PusherEvent emittedEvent = null;
            PusherEvent emittedEvent2 = null;

            var myAction = new Action<PusherEvent>(o => emittedEvent = o);
            var myAction2 = new Action<PusherEvent>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("pusher event listener event", myAction);
            emitter.Bind("pusher event listener event", myAction2);
            emitter.Unbind("pusher event listener event", myAction);

            // Act
            emitter.EmitEvent("pusher event listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNotNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredEventName()
        {
            // Arrange
            dynamic emittedEvent = null;
            dynamic emittedEvent2 = null;

            var myAction = new Action<dynamic>(o => emittedEvent = o);
            var myAction2 = new Action<dynamic>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("listener event", myAction);
            emitter.Bind("listener event", myAction2);
            emitter.Unbind("listener event");

            // Act
            emitter.EmitEvent("listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredRawEventName()
        {
            // Arrange
            string emittedEvent = null;
            string emittedEvent2 = null;

            var myAction = new Action<string>(o => emittedEvent = o);
            var myAction2 = new Action<string>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("raw listener event", myAction);
            emitter.Bind("raw listener event", myAction2);
            emitter.Unbind("raw listener event");

            // Act
            emitter.EmitEvent("raw listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredPusherEventEventName()
        {
            // Arrange
            PusherEvent emittedEvent = null;
            PusherEvent emittedEvent2 = null;

            var myAction = new Action<PusherEvent>(o => emittedEvent = o);
            var myAction2 = new Action<PusherEvent>(o => emittedEvent2 = o);

            var emitter = new PusherClient.EventEmitter();
            emitter.Bind("pusher event listener event", myAction);
            emitter.Bind("pusher event listener event", myAction2);
            emitter.Unbind("pusher event listener event");

            // Act
            emitter.EmitEvent("pusher event listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
            Assert.IsNull(emittedEvent2);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredGeneralListener()
        {
            // Arrange
            Tuple<string, dynamic> emittedEvent = null;

            var myAction = new Action<string, dynamic>((e, o) => emittedEvent = new Tuple<string, dynamic>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);
            emitter.UnbindAll();

            // Act
            emitter.EmitEvent("general listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredRawGeneralListener()
        {
            // Arrange
            Tuple<string, string> emittedEvent = null;

            var myAction = new Action<string, string>((e, o) => emittedEvent = new Tuple<string, string>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);
            emitter.UnbindAll();

            // Act
            emitter.EmitEvent("raw general listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
        }

        [Test]
        public void EventEmitterShouldNotEmitAnEventToAnUnregisteredPusherEventGeneralListener()
        {
            // Arrange
            Tuple<string, PusherEvent> emittedEvent = null;

            var myAction = new Action<string, PusherEvent>((e, o) => emittedEvent = new Tuple<string, PusherEvent>(e, o));

            var emitter = new PusherClient.EventEmitter();
            emitter.BindAll(myAction);
            emitter.UnbindAll();

            // Act
            emitter.EmitEvent("pusher event general listener event", CreateTestEvent());

            // Assert
            Assert.IsNull(emittedEvent);
        }

        private PusherEvent CreateTestEvent()
        {
            var dictionary = new Dictionary<string, int>
            {
                {
                    "stuff", 1234
                }
            };

            var data = JsonConvert.SerializeObject(dictionary);

            var testClass = new TestClass
            {
                user_id = "a user",
                channel = "a channel name",
                @event = "channel event",
                data = data
            };

            var raw = JsonConvert.SerializeObject(testClass);

            // This replicates what happens in the connection class
            var jObject = JObject.Parse(raw);

            if (jObject["data"] != null && jObject["data"].Type != JTokenType.String)
                jObject["data"] = jObject["data"].ToString(Formatting.None);

            var jsonMessage = jObject.ToString(Formatting.None);

            var eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);

            if (jObject["data"] != null)
                eventData["data"] = jObject["data"].ToString(Formatting.None); // undo any kind of deserialisation

            return new PusherEvent(eventData, jsonMessage);
        }

        private class TestClass
        {
            public string user_id { get; set; }
            public string channel { get; set; }
            public string @event { get; set; }
            public string data { get; set; }
        }
    }
}
