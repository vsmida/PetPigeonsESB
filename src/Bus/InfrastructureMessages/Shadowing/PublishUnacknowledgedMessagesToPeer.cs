using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Shadowing
{
    [ProtoContract]
    public class PublishUnacknowledgedMessagesToPeer : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string Peer;

        public PublishUnacknowledgedMessagesToPeer(string peer)
        {
            Peer = peer;
        }
    }
}