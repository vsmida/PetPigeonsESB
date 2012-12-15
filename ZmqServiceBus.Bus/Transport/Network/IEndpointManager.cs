using System;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IEndpointManager : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingBusMessage message);
        void PublishMessage(ISendingBusMessage message);
        void RouteMessage(ISendingBusMessage message, string destinationPeer);
        event Action<IReceivedTransportMessage> OnMessageReceived;
    }
}