using System;
using ProtoBuf;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public interface ITransport : IDisposable
    {
        void Initialize();
        void SendMessage(ITransportMessage message);
        void PublishMessage(ITransportMessage message);
        void RouteMessage(ITransportMessage message);
        void AckMessage(byte[] recipientIdentity, Guid messageId, bool success);
        event Action<ITransportMessage> OnMessageReceived;
        TransportConfiguration Configuration { get; }

        void RegisterPeer(IServicePeer peer);
    }
}