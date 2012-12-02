using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Dispatch
{
    public interface IMessageDispatcher : IDisposable
    {
        void Dispatch(IMessage message);
    }
}