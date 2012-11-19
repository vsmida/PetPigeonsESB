using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface ISubscriptionManager
    {
        event Action<Type> OnNewEventSubscription;
        void StartListeningTo<T>() where T : IEvent;
        void StartHandling<T>() where T : ICommand;

    }
}