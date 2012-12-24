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
        void SendMessage(ISendingBusMessage message, IEndpoint endpoint);
        void SendMessage(ISendingBusMessage message, IEnumerable<IEndpoint> endpoint);
    }
}