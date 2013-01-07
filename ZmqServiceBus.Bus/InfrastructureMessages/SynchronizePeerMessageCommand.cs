using ProtoBuf;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class SynchronizePeerMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly string OriginatingPeer;

        public SynchronizePeerMessageCommand(string messageType, string originatingPeer)
        {
            MessageType = messageType;
            OriginatingPeer = originatingPeer;
        }
    }
}