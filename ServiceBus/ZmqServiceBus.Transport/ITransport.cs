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
        void AckMessage(byte[] recipientIdentity, Guid messageId, bool success);
        void RegisterPublisherEndpoint<T>(string endpoint) where T : IMessage;
        void RegisterCommandHandlerEndpoint<T>(string endpoint) where T : IMessage;
        event Action<ITransportMessage> OnMessageReceived;
        TransportConfiguration Configuration { get; }

    }
}