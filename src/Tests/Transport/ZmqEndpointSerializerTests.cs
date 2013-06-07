using Bus.Transport.Network;
using NUnit.Framework;

namespace Tests.Transport
{
    [TestFixture]
    public class ZmqEndpointSerializerTests
    {
        private ZmqEndpointSerializer _serializer;

        [SetUp]
        public void setup()
        {
            _serializer = new ZmqEndpointSerializer();
        }

        [Test]
        public void should_serialize_deserialize()
        {
            var endpoint = new ZmqEndpoint("test");
            var serialized = _serializer.Serialize(endpoint);
            var deserialized = _serializer.Deserialize(serialized);
            Assert.AreEqual(endpoint, deserialized);
        }
    }
}