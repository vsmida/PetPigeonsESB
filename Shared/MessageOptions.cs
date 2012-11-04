using ProtoBuf;

namespace Shared
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(2, IsRequired = true)]
        public readonly string BrokerName;

        public MessageOptions(ReliabilityLevel reliabilityLevel, string brokerName)
        {
            ReliabilityLevel = reliabilityLevel;
            BrokerName = brokerName;
        }
    }
}