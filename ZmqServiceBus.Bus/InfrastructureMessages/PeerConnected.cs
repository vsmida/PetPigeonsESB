using ProtoBuf;
using Shared;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServicePeer))]
    public class PeerConnected
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly IServicePeer Peer;

        public PeerConnected(IServicePeer peer)
        {
            Peer = peer;
        }
    }
}