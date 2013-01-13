using System;
using Bus.MessageInterfaces;

namespace Bus
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