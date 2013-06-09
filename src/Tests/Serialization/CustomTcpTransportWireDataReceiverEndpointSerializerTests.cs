using System.Net;
using Bus.Transport.Network;
using NUnit.Framework;

namespace Tests.Serialization
{
    [TestFixture]
    public class CustomTcpTransportWireDataReceiverEndpointSerializerTests
    {
        private CustomTcpWireDataReceiverEndpointSerializer _serializer;

        [SetUp]
        public void setup()
        {
            _serializer = new CustomTcpWireDataReceiverEndpointSerializer();
        }

        [Test]
        public void should_serialize_deserialize()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 22);
            var customTcpWireDataReceiverEndpoint = new CustomTcpWireDataReceiverEndpoint(endpoint);
            var ser = _serializer.Serialize(customTcpWireDataReceiverEndpoint);
            var deserialized = _serializer.Deserialize(ser);
            Assert.AreEqual(customTcpWireDataReceiverEndpoint, deserialized);

        }
    }
}