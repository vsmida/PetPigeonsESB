using System.IO;
using System.Text;
using ProtoBuf;
using Shared;

namespace Bus.Transport.Network
{
    public class ZmqEndpointSerializer : EndpointSerializer<ZmqEndpoint>
    {
        public override Stream Serialize(ZmqEndpoint zmqEndpoint)
        {
            var length = new byte[4];
            var stringEndpoint = zmqEndpoint.Endpoint;
            ByteUtils.WriteInt(length, 0, stringEndpoint.Length);
            var memoryStream = new MemoryStream(4 + stringEndpoint.Length);
            memoryStream.Write(length, 0, 4);
            var endpoint = Encoding.ASCII.GetBytes(stringEndpoint);
            memoryStream.Write(endpoint, 0, endpoint.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public override ZmqEndpoint Deserialize(Stream stream)
        {
            var length = ByteUtils.ReadIntFromStream(stream);
            var endpointBytes = new byte[length];
            stream.Read(endpointBytes, 0, length);
            return new ZmqEndpoint(Encoding.ASCII.GetString(endpointBytes));
        }
    }

    [ProtoContract]
    public class ZmqEndpoint : IEndpoint
    {
        protected bool Equals(ZmqEndpoint other)
        {
            return string.Equals(Endpoint, other.Endpoint);
        }

        public bool Equals(IEndpoint other)
        {
            return Equals((object)other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ZmqEndpoint)obj);
        }

        public override int GetHashCode()
        {
            return (Endpoint != null ? Endpoint.GetHashCode() : 0);
        }

        private const WireTransportType _wireSendingTransportType = Network.WireTransportType.ZmqPushPullTransport;

        [ProtoMember(1, IsRequired = true)]
        public string Endpoint { get; set; }

        public bool IsMulticast
        {
            get { return false; }
        }



        public ZmqEndpoint(string endpoint)
        {
            Endpoint = endpoint;
        }

        private ZmqEndpoint()
        {

        }

        public WireTransportType WireTransportType
        {
            get
            {
                return _wireSendingTransportType;
            }
        }
        public override string ToString()
        {
            return Endpoint;
        }

    }
}