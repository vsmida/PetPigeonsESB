using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [InfrastructureMessage]
    [ProtoContract]
    public class RegisterPeerCommand : ICommand
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