using Shared;

namespace ZmqServiceBus.Bus
{
    public interface IBus
    {
        void Send(ICommand command);
        void Publish(IEvent message);
        void Initialize();
    }
}