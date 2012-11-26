using ProtoBuf;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class SynchronizeMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;

        public SynchronizeMessageCommand(string messageType)
        {
            MessageType = messageType;
        }
    }
}