using System;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.Serializer;
using Bus.Transport.Network;
using Moq;
using NUnit.Framework;

namespace Tests.Serialization
{
    [TestFixture]
    public class CompletionAcknowlegementMessageSerializerTests
    {
        private CompletionAcknowledgementMessageSerializer _serializer;
        private Mock<IAssemblyScanner> _assemblyScannerMock;

        [SetUp]
        public void setup()
        {
            _assemblyScannerMock = new Mock<IAssemblyScanner>();
            _serializer = new CompletionAcknowledgementMessageSerializer(_assemblyScannerMock.Object);
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