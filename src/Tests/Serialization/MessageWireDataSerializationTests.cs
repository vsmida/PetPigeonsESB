using System;
using System.Collections.Generic;
using System.Text;
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
            _serializer = new MessageWireDataSerializer(new AssemblyScanner());
        }

        [Test]
        public void should_serialize_deserialize()
        {
            var message = new MessageWireData(typeof(TestData.FakeCommand).FullName, Guid.NewGuid(), "peer", Encoding.ASCII.GetBytes("data"))
                              {SequenceNumber = 1234567};
            var buffer = _serializer.Serialize(message);
            var stream = new FrameStream(new List<Frame>{new Frame(buffer,0, buffer.Length)});
            var deserializedMessage =_serializer.Deserialize(stream);

            Assert.AreEqual(message.Data, deserializedMessage.Data);
            Assert.AreEqual(message.MessageIdentity, deserializedMessage.MessageIdentity);
            Assert.AreEqual(message.MessageType, deserializedMessage.MessageType);
            Assert.AreEqual(message.SendingPeer, deserializedMessage.SendingPeer);
            Assert.AreEqual(message.SequenceNumber, deserializedMessage.SequenceNumber);
        }
    }
}