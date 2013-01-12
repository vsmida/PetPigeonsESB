using ProtoBuf;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.InfrastructureMessages
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