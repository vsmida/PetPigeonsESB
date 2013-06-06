using System;
using Bus.InfrastructureMessages;
using Bus.Serializer;
using Bus.Transport.Network;
using NUnit.Framework;

namespace Tests.Serialization
{
    [TestFixture]
    public class CompletionAcknowlegementMessageSerializerTests
    {
        private CompletionAcknowledgementMessageSerializer _serializer;

        [SetUp]
        public void setup()
        {
            _serializer = new CompletionAcknowledgementMessageSerializer();
        }

        [Test]
        public void should_serialize_deserialize()
        {
            var message = new CompletionAcknowledgementMessage(Guid.NewGuid(), "type", true, new ZmqEndpoint("toto"));

            var data = _serializer.Serialize(message);
            var deserialized = _serializer.Deserialize(data);

            Assert.AreEqual(message.Endpoint, deserialized.Endpoint);
            Assert.AreEqual(message.MessageId, deserialized.MessageId);
            Assert.AreEqual(message.MessageType, deserialized.MessageType);
            Assert.AreEqual(message.ProcessingSuccessful, deserialized.ProcessingSuccessful);
        }
    }
}