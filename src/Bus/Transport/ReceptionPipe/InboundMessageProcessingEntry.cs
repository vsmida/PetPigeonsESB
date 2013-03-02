using System.Collections.Generic;
using Bus.MessageInterfaces;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundMessageProcessingEntry
    {
        public ReceivedTransportMessage InitialTransportMessage = new ReceivedTransportMessage();
        public bool ForceMessageThrough;
        public IBusEventProcessorCommand Command;

        public List<InboundBusinessMessageEntry> QueuedInboundEntries;
        public InboundInfrastructureEntry InfrastructureEntry = new InboundInfrastructureEntry();
        public InboundBusinessMessageEntry InboundBusinessMessageEntry = new InboundBusinessMessageEntry();
        public bool IsInfrastructureMessage;
        public bool IsStrandardMessage;
        public bool IsCommand;
    }
}