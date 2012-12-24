using System;
using ProtoBuf;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IMessageSubscription
    {
         Type MessageType { get; }
         IEndpoint Endpoint { get; }
         ISubscriptionFilter SubscriptionFilter { get; }
         string Peer { get;}

    }

    [ProtoContract]
    [ProtoInclude(4, typeof(DummySubscriptionFilter))]
    public class MessageSubscription : IMessageSubscription
    {


        [ProtoMember(1, IsRequired = true)]
        public Type MessageType { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public string Peer { get; private set; }
        [ProtoMember(3, IsRequired = true)]
        private ZmqEndpoint _endpoint;

        public IEndpoint Endpoint
        {
            get { return _endpoint; }
            private set { _endpoint = (ZmqEndpoint) value; }
        }

        [ProtoMember(4, IsRequired = true)]
        private DummySubscriptionFilter _subscriptionFilter;
        public ISubscriptionFilter SubscriptionFilter
        {
            get { return _subscriptionFilter; }
            private set { _subscriptionFilter = (DummySubscriptionFilter) value; }
        }

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