using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.BusEventProcessorCommands
{
    class DisconnectEndpoint : IBusEventProcessorCommand
    {
        public readonly IEndpoint Endpoint;

        public DisconnectEndpoint(IEndpoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}