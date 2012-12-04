using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Dispatch
{
    public interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}