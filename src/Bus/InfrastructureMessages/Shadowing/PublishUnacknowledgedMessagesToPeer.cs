using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Shadowing
{
    [ProtoContract]
    class PublishUnacknowledgedMessagesToPeer : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly PeerId Peer;

        public PublishUnacknowledgedMessagesToPeer(PeerId peer)
        {
            Peer = peer;
        }
    }
}