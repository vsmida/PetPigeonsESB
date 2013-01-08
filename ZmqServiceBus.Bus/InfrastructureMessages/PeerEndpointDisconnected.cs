using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class PeerEndpointDisconnected : IMessage
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