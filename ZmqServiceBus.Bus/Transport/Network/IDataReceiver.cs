using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Disruptor;
using Disruptor.Dsl;
using Shared;
using StructureMap;
using ZeroMQ.Monitoring;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using IMessageSender = ZmqServiceBus.Bus.Transport.SendingPipe.IMessageSender;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataReceiver : IDisposable
    {
        void Initialize(RingBuffer<InboundMessageProcessingEntry> disruptor);
        void InjectMessage(ReceivedTransportMessage message);
    }
    
    public class DataReceiver : IDataReceiver
    {
        private readonly IWireReceiverTransport[] _transports;
        private RingBuffer<InboundMessageProcessingEntry> _ringBuffer;


        public DataReceiver(IWireReceiverTransport[] transports)
        {
            _transports = transports;
        }

        public void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer)
        {
            _ringBuffer = ringBuffer;
            foreach (IWireReceiverTransport wireReceiverTransport in _transports)
            {
                wireReceiverTransport.Initialize(ringBuffer);
            }
        }

        public void InjectMessage(ReceivedTransportMessage message)
        {
            var sequence = _ringBuffer.Next();
            var entry = _ringBuffer[sequence];
            entry.InitialTransportMessage = message;
            _ringBuffer.Publish(sequence);
        }

        public void Dispose()
        {
            foreach (IWireReceiverTransport wireReceiverTransport in _transports)
            {
                wireReceiverTransport.Dispose();
            }
        }
    }
}