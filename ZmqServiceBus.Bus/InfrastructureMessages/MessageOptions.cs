using ProtoBuf;
using Shared;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;

        public MessageOptions(string messageType, ReliabilityLevel ReliabilityLevel)
        {
            MessageType = messageType;
            ReliabilityLevel = ReliabilityLevel;
        }

        private MessageOptions(){}
    }
}