using ProtoBuf;

namespace Shared
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(3, IsRequired = true)]
        public readonly string BrokerName;

        public MessageOptions(string messageType, ReliabilityLevel reliabilityLevel, string brokerName)
        {
            MessageType = messageType;
            ReliabilityLevel = reliabilityLevel;
            BrokerName = brokerName;
        }
    }
}