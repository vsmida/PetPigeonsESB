using System;
using Bus.Transport.SendingPipe;

namespace Bus.Transport.Network
{
    public class CustomWireSendingTransport
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public event Action<IEndpoint> EndpointDisconnected;
        public WireTransportType TransportType { get; private set; }
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void SendMessage(WireSendingMessage message, IEndpoint endpoint)
        {
            throw new NotImplementedException();
        }

        public void DisconnectEndpoint(IEndpoint endpoint)
        {
            throw new NotImplementedException();
        }
    }
}