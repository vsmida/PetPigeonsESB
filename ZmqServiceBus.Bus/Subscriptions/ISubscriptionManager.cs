using System;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Subscriptions
{
    public interface ISubscriptionManager
    {
        event Action<Type> EventUnsubscibe;
        IDisposable StartListeningTo<T>() where T : IEvent;
        IDisposable StartListeningTo(Type eventType);
    }
}