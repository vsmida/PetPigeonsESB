using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Subscriptions
{
    public interface ISubscriptionFilter
    {
        bool Matches(IMessage item);
    }
}