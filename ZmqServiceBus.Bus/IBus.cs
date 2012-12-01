using System;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IBus : IDisposable
    {
        IBlockableUntilCompletion Send(ICommand command);
        void Publish(IEvent message);
        void Initialize();
    }

    public interface IReplier
    {
        void Reply(IMessage message);
    }
}