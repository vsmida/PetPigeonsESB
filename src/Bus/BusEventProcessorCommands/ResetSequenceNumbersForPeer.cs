using Bus.MessageInterfaces;

namespace Bus.BusEventProcessorCommands
{
    class ResetSequenceNumbersForPeer : IBusEventProcessorCommand
    {
        public readonly PeerId PeerId;

        public ResetSequenceNumbersForPeer(PeerId peerId)
        {
            PeerId = peerId;
        }
    }
}