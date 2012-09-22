using System;
using ProtoBuf;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public interface ITransport : IDisposable
    {
        void Initialize();
        void SendMessage<T>(T message) where T : IMessage;
        void PublishMessage<T>(T message) where T : IMessage;
        void RegisterPublisherEndpoint<T>(string endpoint) where T : IMessage;
        void RegisterCommandHandlerEndpoint<T>(string endpoint) where T : IMessage;
        event Action<ITransportMessage> OnMessageReceived;

    }
}