namespace ZmqServiceBus.Contracts
{
    public interface IEventHandler<T>  : IMessageHandler<T> where T : IEvent
    {
        void Handle(T message);
    }
}