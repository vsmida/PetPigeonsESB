using ProtoBuf;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Subscriptions
{
    [ProtoContract]
    public class DummySubscriptionFilter : ISubscriptionFilter
    {
        public bool Matches(IMessage item)
        {
            return true;
        }
    }
}