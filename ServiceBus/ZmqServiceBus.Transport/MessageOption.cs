using ProtoBuf;

namespace ZmqServiceBus.Transport
{
    [ProtoContract]
    public class MessageOption
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ReliabilityOption ReliabilityLevel;
        [ProtoMember(2, IsRequired = true)]
        public readonly string BrokerName;

        public MessageOption(ReliabilityOption reliabilityLevel, string brokerName)
        {
            ReliabilityLevel = reliabilityLevel;
            BrokerName = brokerName;
        }
    }
}