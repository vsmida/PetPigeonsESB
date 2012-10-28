﻿using System;
using ProtoBuf;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public interface ITransport : IDisposable
    {
        void Initialize(string serviceIdentity);
        void SendMessage(ITransportMessage message, IQosStrategy strategy);
        void PublishMessage(ITransportMessage message, IQosStrategy strategy); 
        void AckMessage(string recipientIdentity, Guid messageId, bool success);
        void RegisterPublisherEndpoint<T>(string endpoint) where T : IMessage;
        void RegisterCommandHandlerEndpoint<T>(string endpoint) where T : IMessage;
        event Action<ITransportMessage> OnMessageReceived;
        TransportConfiguration Configuration { get; }

    }
}