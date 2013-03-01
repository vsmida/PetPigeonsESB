using System.Collections.Generic;
using Bus.MessageInterfaces;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundMessageProcessingEntry
    {
        public ReceivedTransportMessage InitialTransportMessage = new ReceivedTransportMessage();
        public bool ForceMessageThrough;
        public IBusEventProcessorCommand Command;

        public List<InboundBusinessMessageEntry> InboundEntries;
        public InboundInfrastructureEntry InfrastructureEntry;
    }
}