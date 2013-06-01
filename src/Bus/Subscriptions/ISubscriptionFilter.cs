using System;
using Bus.MessageInterfaces;

namespace Bus.Subscriptions
{
    public interface ISubscriptionFilter
    {
        bool Matches(IMessage item);
    }

    public interface ISubscriptionFilter<T> : ISubscriptionFilter
    {
        
    }
}