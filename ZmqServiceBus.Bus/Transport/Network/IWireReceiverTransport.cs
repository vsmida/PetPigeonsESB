using System;
using System.Collections.Concurrent;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IWireReceiverTransport : IDisposable
    {
        void Initialize(BlockingCollection<IReceivedTransportMessage> messageQueue);
    }
}