using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ISubscriptionFilter
    {
        bool Matches(IMessage item);
    }
}