using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class PublishUnacknowledgedMessagesToPeerForTransport : ICommand
    {
        public readonly string Peer;
        public readonly WireTransportType[] TransportType;

        public PublishUnacknowledgedMessagesToPeerForTransport(string peer, WireTransportType[] transportType)
        {
            Peer = peer;
            TransportType = transportType;
        }
    }
}