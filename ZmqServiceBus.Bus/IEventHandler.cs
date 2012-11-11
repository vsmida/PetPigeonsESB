using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IEventHandler<T>  : IMessageHandler<T> where T : IEvent
    {
        void Handle(T message);
    }
}