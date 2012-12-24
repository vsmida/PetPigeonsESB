using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class RegisterPeerCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public RegisterPeerCommand(ServicePeer peer)
        {
            Peer = peer;
        }
    }
}