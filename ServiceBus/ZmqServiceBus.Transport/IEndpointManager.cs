using System;
using ProtoBuf;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public interface IEndpointManager : IDisposable
    {
        void Initialize();
        void SendMessage(ITransportMessage message);
        void PublishMessage(ITransportMessage message);
        void RouteMessage(ITransportMessage message);
        event Action<ITransportMessage> OnMessageReceived;
        void RegisterPeer(IServicePeer peer);
    }
}