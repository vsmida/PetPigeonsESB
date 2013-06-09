using System;
using Bus.Subscriptions;
using Bus.Transport.Network;
using ProtoBuf;
using Shared;
namespace Bus.Transport
{
    [ProtoContract]
    public class MessageSubscription
    {
        [ProtoMember(1, IsRequired = true)]
        public Type MessageType { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public PeerId Peer { get; private set; }
        [ProtoMember(3, IsRequired = true)]
        public readonly IEndpoint Endpoint;
        [ProtoMember(4, IsRequired = true)] 
        public readonly ISubscriptionFilter SubscriptionFilter;
        [ProtoMember(5, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;

        public MessageSubscription(Type messageType, PeerId peer, IEndpoint endpoint, ISubscriptionFilter subscriptionFilter, ReliabilityLevel reliabilityLevel)
        {
            MessageType = messageType;
            Peer = peer;
            Endpoint = endpoint;
            SubscriptionFilter = (subscriptionFilter);
            ReliabilityLevel = reliabilityLevel;
        }

        private MessageSubscription() { }
    }
}