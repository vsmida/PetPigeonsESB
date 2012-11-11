using System;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IEndpointManager : IDisposable
    {
        void Initialize();
        void SendMessage(IReceivedTransportMessage message);
        void PublishMessage(IReceivedTransportMessage message);
        void RouteMessage(IReceivedTransportMessage message, string destinationPeer);
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void RegisterPeer(IServicePeer peer);
    }
}