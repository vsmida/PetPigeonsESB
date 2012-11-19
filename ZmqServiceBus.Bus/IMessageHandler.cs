using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IMessageHandler<in T> where T : IMessage
    {
        void Handle(T message);
    }



}