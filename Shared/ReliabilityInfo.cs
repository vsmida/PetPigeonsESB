using ProtoBuf;

namespace Shared
{
    [ProtoContract]
    public class ReliabilityInfo
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(2, IsRequired = true)]
        public readonly string BrokerName;


        public ReliabilityInfo(ReliabilityLevel reliabilityLevel, string brokerName = null)
        {
            BrokerName = brokerName;
            ReliabilityLevel = reliabilityLevel;
        }

        private ReliabilityInfo()
        {
        }
    }
}