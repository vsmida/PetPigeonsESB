using System;
using Bus.MessageInterfaces;

namespace Bus.Subscriptions
{
    interface ISubscriptionManager
    {
        event Action<Type> EventUnsubscibe;
        IDisposable StartListeningTo<T>() where T : IEvent;
        IDisposable StartListeningTo(Type eventType);
    }
}