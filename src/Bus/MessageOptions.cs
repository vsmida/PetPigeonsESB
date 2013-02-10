using System;
using Bus.Subscriptions;
using Bus.Transport.Network;
using ProtoBuf;
using Shared;

namespace Bus
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Type MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(3, IsRequired = true)]
        public readonly WireTransportType TransportType;
        [ProtoMember(4, IsRequired = true)]
        public readonly ISubscriptionFilter SubscriptionFilter;

        public MessageOptions(Type messageType, ReliabilityLevel reliabilityLevel, WireTransportType transportType, ISubscriptionFilter subscriptionFilter)
        {
            MessageType = messageType;
            ReliabilityLevel = reliabilityLevel;
            TransportType = transportType;
            SubscriptionFilter = subscriptionFilter;
        }

        private MessageOptions()
        {
        }
    }


}