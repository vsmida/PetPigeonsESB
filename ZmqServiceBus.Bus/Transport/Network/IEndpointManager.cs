using System;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IEndpointManager : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingTransportMessage message);
        void PublishMessage(ISendingTransportMessage message);
        void RouteMessage(ISendingTransportMessage message, string destinationPeer);
        event Action<IReceivedTransportMessage> OnMessageReceived;
    }
}