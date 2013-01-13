using Bus.MessageInterfaces;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundMessageProcessingEntry
    {
        public ReceivedTransportMessage InitialTransportMessage;
        public bool ForceMessageThrough;
        public IBusEventProcessorCommand Command;
    }
}