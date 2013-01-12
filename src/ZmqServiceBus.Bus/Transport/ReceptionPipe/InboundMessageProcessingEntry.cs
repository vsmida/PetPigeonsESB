using Disruptor;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class InboundMessageProcessingEntry
    {
        public ReceivedTransportMessage InitialTransportMessage;
        public bool ForceMessageThrough;
        public IBusEventProcessorCommand Command;
    }
}