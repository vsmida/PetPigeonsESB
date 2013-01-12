using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus
{
    public interface IBusEventHandler<T>  : IMessageHandler where T : IEvent
    {
        void Handle(T message);
    }
}