using System;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Dispatch
{
    public interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}