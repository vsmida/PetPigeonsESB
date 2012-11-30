using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IEventHandler<T>  : IMessageHandler where T : IEvent
    {
        void Handle(T message);
    }
}