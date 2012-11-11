using System;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IEndpointManager : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingTransportMessage message);
        void PublishMessage(ISendingTransportMessage message);
        void RouteMessage(ISendingTransportMessage message, string destinationPeer);
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void RegisterPeer(IServicePeer peer);
    }
}