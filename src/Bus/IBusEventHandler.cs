using Bus.MessageInterfaces;

namespace Bus
{
    public interface IBusEventHandler<T>  : IMessageHandler where T : IEvent
    {
        void Handle(T message);
    }
}