using System;
using System.Linq;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface ISubscriptionManager
    {
        event Action<Type> NewEventSubscription;
        event Action<Type> EventUnsubscibe;
        IDisposable StartListeningTo<T>() where T : IEvent;

    }
}