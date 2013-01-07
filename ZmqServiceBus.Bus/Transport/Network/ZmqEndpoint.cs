using ProtoBuf;

namespace ZmqServiceBus.Bus.Transport.Network
{
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
            return Equals((ZmqEndpoint) obj);
        }

        public override int GetHashCode()
        {
            return (Endpoint != null ? Endpoint.GetHashCode() : 0);
        }

        private const WireSendingTransportType _wireSendingTransportType = WireSendingTransportType.ZmqPushTransport;

        [ProtoMember(1, IsRequired = true)]
        public string Endpoint { get; set; }

        public ZmqEndpoint(string endpoint)
        {
            Endpoint = endpoint;
        }

        private ZmqEndpoint()
        {

        }

        public WireSendingTransportType WireTransportType
        {
            get
            {
                return _wireSendingTransportType;
            }
        }


    }
}