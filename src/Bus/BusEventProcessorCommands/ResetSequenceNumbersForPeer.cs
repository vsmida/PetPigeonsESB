using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.BusEventProcessorCommands
{
    public class ResetSequenceNumbersForPeer : IBusEventProcessorCommand
    {
        public readonly string PeerName;

        public ResetSequenceNumbersForPeer(string peerName)
        {
            PeerName = peerName;
        }
    }

    public class DisconnectEndpoint : IBusEventProcessorCommand
    {
        public readonly IEndpoint Endpoint;

        public DisconnectEndpoint(IEndpoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}