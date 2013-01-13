using System;
using Bus.Subscriptions;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.Transport
{

    [ProtoContract]
    public class MessageSubscription
    {

        [ProtoMember(1, IsRequired = true)]
        public Type MessageType { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public string Peer { get; private set; }
        [ProtoMember(3, IsRequired = true)]
        public readonly IEndpoint Endpoint;
        [ProtoMember(4, IsRequired = true)]
        public readonly ISubscriptionFilter SubscriptionFilter;

        public MessageSubscription(Type messageType, string peer, IEndpoint endpoint, ISubscriptionFilter subscriptionFilter)
        {
            MessageType = messageType;
            Peer = peer;
            Endpoint = endpoint;
            SubscriptionFilter = subscriptionFilter;
        }

        private MessageSubscription(){}
    }
}