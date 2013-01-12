using System;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [InfrastructureMessage]
    public class CompletionAcknowledgementMessage : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;
        [ProtoMember(2, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(3, IsRequired = true)]
        public readonly bool ProcessingSuccessful;
        [ProtoMember(4, IsRequired = true)]
        public readonly WireTransportType TransportType;

        public CompletionAcknowledgementMessage(Guid messageId,string messageType, bool processingSuccessful, WireTransportType transportType)
        {
            MessageId = messageId;
            ProcessingSuccessful = processingSuccessful;
            TransportType = transportType;
            MessageType = messageType;
        }
    }
}