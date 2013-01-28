using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.InfrastructureMessages.Heartbeating
{
    class PeerEndpointDisconnected : IMessage
    {
        public readonly string Peer;
        public readonly IEndpoint Endpoint;

        public PeerEndpointDisconnected(string peer, IEndpoint endpoint)
        {
            Peer = peer;
            Endpoint = endpoint;
        }
    }
}