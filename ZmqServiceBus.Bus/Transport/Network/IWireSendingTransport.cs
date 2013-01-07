using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IWireSendingTransport : IDisposable
    {
        event Action<IEndpoint> EndpointDisconnected;
        WireSendingTransportType TransportType { get; }
        void Initialize();
        void SendMessage(WireSendingMessage message, IEndpoint endpoint);
        void SendMessage(WireSendingMessage message, IEnumerable<IEndpoint> endpoint);
    }
}