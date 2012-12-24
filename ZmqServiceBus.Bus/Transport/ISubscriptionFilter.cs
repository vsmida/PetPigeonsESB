using ProtoBuf;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ISubscriptionFilter
    {
        bool Matches(IMessage item);
    }

    [ProtoContract]
    public class DummySubscriptionFilter : ISubscriptionFilter
    {
        public bool Matches(IMessage item)
        {
            return true;
        }
    }
}