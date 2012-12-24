using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class ReliabilityInfo
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(2, IsRequired = true)]
        public readonly string BrokerName;
        [ProtoMember(3, IsRequired = true)]
        public readonly IEndpoint BrokerEndpoint;


        public ReliabilityInfo(ReliabilityLevel reliabilityLevel, string brokerName = null, IEndpoint brokerEndpoint = null)
        {
            BrokerName = brokerName;
            BrokerEndpoint = brokerEndpoint;
            ReliabilityLevel = reliabilityLevel;
        }

        private ReliabilityInfo(IEndpoint brokerEndpoint)
        {
            BrokerEndpoint = brokerEndpoint;
        }
    }
}