using System;
using System.Collections.Concurrent;
using Disruptor;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IWireReceiverTransport : IDisposable
    {
        void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer);
        WireTransportType TransportType { get; }
    }
}