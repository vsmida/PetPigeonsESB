using System;
using Bus.Transport.ReceptionPipe;
using Disruptor;

namespace Bus.Transport.Network
{
    public interface IWireReceiverTransport : IDisposable
    {
        void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer);
        WireTransportType TransportType { get; }
    }
}