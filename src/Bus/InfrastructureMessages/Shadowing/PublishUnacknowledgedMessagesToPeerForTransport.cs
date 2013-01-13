using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.InfrastructureMessages.Shadowing
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