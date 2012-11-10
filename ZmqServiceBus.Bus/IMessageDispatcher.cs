using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}