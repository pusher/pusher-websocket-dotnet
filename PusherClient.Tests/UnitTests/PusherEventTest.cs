using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;

namespace PusherClient.Tests.UnitTests
{
    [TestFixture]
    public class PusherEventTest
    {
        [Test]
        public void PusherEventShouldPopulateCorrectlyWhenGivenAnEventDataDictionaryAndRawJsonString()
        {
            // Arrange
            var testData = GetTestData("cunning user name","a channel","Event name");
            var rawJson = JsonConvert.SerializeObject(testData);

            // Act
            var pusherEvent = CreateTestEvent(testData);

            // Assert
            Assert.AreEqual("cunning user name", pusherEvent.UserId);
            Assert.AreEqual("a channel", pusherEvent.ChannelName);
            Assert.AreEqual("Event name", pusherEvent.EventName);
            Assert.AreEqual("stuff", pusherEvent.Data);
            Assert.AreEqual(rawJson, pusherEvent.ToString());
        }

        [Test]
        public void PusherEventShouldRetrieveAnAdditionalPropertyThatIsNotAvailableAsPropertyOnItself()
        {
            var testData = GetExtendedTestData("cunning user name", "a channel", "Event name");

            // Act
            var pusherEvent = CreateTestEvent(testData);

            // Assert
            Assert.AreEqual("cunning user name", pusherEvent.UserId);
            Assert.AreEqual("a channel", pusherEvent.ChannelName);
            Assert.AreEqual("Event name", pusherEvent.EventName);
            Assert.AreEqual("more stuff", pusherEvent.Data);
            Assert.AreEqual("an extra property", pusherEvent.GetProperty("ExtraProperty").ToString());
        }

        [Test]
        public void PusherEventShouldReturnNullWhenAskedToRetrieveAPropertyThatIsNotAvailableAsPropertyOnItself()
        {
            var testData = GetTestData("cunning user name", "a channel", "Event name");

            // Act
            var pusherEvent = CreateTestEvent(testData);

            // Assert
            Assert.AreEqual("cunning user name", pusherEvent.UserId);
            Assert.AreEqual("a channel", pusherEvent.ChannelName);
            Assert.AreEqual("Event name", pusherEvent.EventName);
            Assert.AreEqual("stuff", pusherEvent.Data);
            Assert.IsNull(pusherEvent.GetProperty("ExtraProperty"));
        }

        private PusherClient.PusherEvent CreateTestEvent(TestClass testClass)
        {
            var raw = JsonConvert.SerializeObject(testClass);
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw);

            var template = new { @event = string.Empty, data = string.Empty, channel = string.Empty };

            var message = JsonConvert.DeserializeAnonymousType(raw, template);


            return new PusherClient.PusherEvent(properties, raw);
        }

        private TestClass GetTestData(string username, string channel, string eventName)
        {
            return new TestClass
            {
                user_id = username,
                channel = channel,
                @event = eventName,
                data = "stuff"
            };
        }

        private ExtendedTestClass GetExtendedTestData(string username, string channel, string eventName)
        {
            return new ExtendedTestClass
            {
                user_id = username,
                channel = channel,
                @event = eventName,
                data = "more stuff",
                ExtraProperty = "an extra property"
            };
        }

        private class TestClass
        {
            public string user_id { get; set; }
            public string channel { get; set; }
            public string @event { get; set; }
            public string data { get; set; }
        }

        private class ExtendedTestClass : TestClass
        {
            public string ExtraProperty { get; set; }
        }
    }
}
