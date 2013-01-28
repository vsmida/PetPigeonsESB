using Bus.MessageInterfaces;
using Bus.Transport;
using ProtoBuf;
using Shared;
using Shared.Attributes;

namespace Bus.InfrastructureMessages.Topology
{
    [InfrastructureMessage]
    [ProtoContract]
    class RegisterPeerCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public RegisterPeerCommand(ServicePeer peer)
        {
            Peer = peer;
        }

        public ReliabilityLevel DesiredReliability
        {
            get { return ReliabilityLevel.FireAndForget; }
        }
    }
}