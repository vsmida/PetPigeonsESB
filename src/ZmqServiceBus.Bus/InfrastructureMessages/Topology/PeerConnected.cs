using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServicePeer))]
    public class PeerConnected : IEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public PeerConnected(ServicePeer peer)
        {
            Peer = peer;
        }

    }
}