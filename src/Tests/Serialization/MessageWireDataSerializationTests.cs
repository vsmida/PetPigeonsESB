using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bus;
using Bus.Dispatch;
using Bus.Serializer;
using Bus.Transport.SendingPipe;
using NUnit.Framework;
using PgmTransport;
using Tests.Transport;

namespace Tests.Serialization
{
    [TestFixture]
    public class MessageWireDataSerializationTests
    {
        private MessageWireDataSerializer _serializer;
        [SetUp]
        public void setup()
        {
            _serializer = new MessageWireDataSerializer(new SerializationHelper(new AssemblyScanner()));
        }

        [Test]
        public void should_serialize_deserialize()
        {
            var message = new MessageWireData(typeof(TestData.FakeCommand).FullName, Guid.NewGuid(), new PeerId(3), Encoding.ASCII.GetBytes("data"))
                              {SequenceNumber = 1234567};
            var buffer = _serializer.Serialize(message);
            var stream = new MemoryStream(buffer,0, buffer.Length);
            var deserializedMessage = new MessageWireData();
            _serializer.Deserialize(stream, deserializedMessage);

            Assert.AreEqual(message.Data, deserializedMessage.Data);
            Assert.AreEqual(message.MessageIdentity, deserializedMessage.MessageIdentity);
            Assert.AreEqual(message.MessageType, deserializedMessage.MessageType);
            Assert.AreEqual(message.SendingPeerId, deserializedMessage.SendingPeerId);
            Assert.AreEqual(message.SequenceNumber, deserializedMessage.SequenceNumber);
        }
    }
}