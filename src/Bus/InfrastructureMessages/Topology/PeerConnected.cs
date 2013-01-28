using Bus.MessageInterfaces;
using Bus.Transport;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Topology
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServicePeer))]
    class PeerConnected : IEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public PeerConnected(ServicePeer peer)
        {
            Peer = peer;
        }

    }
}