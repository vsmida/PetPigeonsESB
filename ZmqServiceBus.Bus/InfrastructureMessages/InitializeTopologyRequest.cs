using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [InfrastructureMessage]
    [ProtoContract]
    public class InitializeTopologyRequest : ICommand
    {
        [ProtoMember(1, IsRequired = true)] public readonly ServicePeer Peer;

        public InitializeTopologyRequest(ServicePeer peer)
        {
            Peer = peer;
        }
    }
}