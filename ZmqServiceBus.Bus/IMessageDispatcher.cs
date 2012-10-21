using Shared;

namespace ZmqServiceBus.Bus
{
    public interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}