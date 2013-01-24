using System;
using Bus.Transport.SendingPipe;

namespace Bus.Transport.Network
{
    public interface IWireSendingTransport : IDisposable
    {
        event Action<IEndpoint> EndpointDisconnected;
        WireTransportType TransportType { get; }
        void Initialize();
        void SendMessage(WireSendingMessage message, IEndpoint endpoint);
        void DisconnectEndpoint(IEndpoint endpoint);
    }
}