using System;
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

    public class MessageSubscription : IMessageSubscription
    {
        public Type MessageType { get; private set; }
        public string Peer { get; private set; }
        public IEndpoint Endpoint { get; private set; }
        public ISubscriptionFilter SubscriptionFilter { get; private set; }
    }
}