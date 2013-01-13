using System;
using Bus.MessageInterfaces;
using Bus.Transport.ReceptionPipe;
using Disruptor;

namespace Bus.Transport.Network
{
    public interface IDataReceiver : IDisposable
    {
        void Initialize(RingBuffer<InboundMessageProcessingEntry> disruptor);
        void InjectMessage(ReceivedTransportMessage message, bool forceMessageThrough = false);
        void InjectCommand(IBusEventProcessorCommand busEventProcessorCommand);
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

        public void InjectMessage(ReceivedTransportMessage message, bool forceMessage = false)
        {
            var sequence = _ringBuffer.Next();
            var entry = _ringBuffer[sequence];
            entry.InitialTransportMessage = message;
            entry.ForceMessageThrough = forceMessage;
            entry.Command = null;
            _ringBuffer.Publish(sequence);
        }

        public void InjectCommand(IBusEventProcessorCommand busEventProcessorCommand)
        {
            var sequence = _ringBuffer.Next();
            var entry = _ringBuffer[sequence];
            entry.InitialTransportMessage = null;
            entry.ForceMessageThrough = false;
            entry.Command = busEventProcessorCommand;
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