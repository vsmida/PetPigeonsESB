using Bus.MessageInterfaces;

namespace Bus.BusEventProcessorCommands
{
    class ResetSequenceNumbersForPeer : IBusEventProcessorCommand
    {
        public readonly string PeerName;

        public ResetSequenceNumbersForPeer(string peerName)
        {
            PeerName = peerName;
        }
    }
}