using Bus.MessageInterfaces;
using Bus.Transport;
using ProtoBuf;
using Shared.Attributes;

namespace Bus.InfrastructureMessages.Topology
{
    [InfrastructureMessage]
    [ProtoContract]
    class InitializeTopologyRequest : ICommand
    {
        [ProtoMember(1, IsRequired = true)] public readonly ServicePeer Peer;

        public InitializeTopologyRequest(ServicePeer peer)
        {
            Peer = peer;
        }
    }
}