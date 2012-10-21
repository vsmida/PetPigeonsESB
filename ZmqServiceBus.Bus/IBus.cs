using System;
using Shared;

namespace ZmqServiceBus.Bus
{
    public interface IBus : IDisposable
    {
        void Send(ICommand command);
        void Publish(IEvent message);
        void Initialize();
    }
}