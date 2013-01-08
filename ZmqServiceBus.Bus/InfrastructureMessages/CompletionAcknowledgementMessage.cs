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
        public readonly bool ProcessingSuccessful;
        [ProtoMember(3, IsRequired = true)]
        public readonly WireTransportType TransportType;

        public CompletionAcknowledgementMessage(Guid messageId, bool processingSuccessful, WireTransportType transportType)
        {
            MessageId = messageId;
            ProcessingSuccessful = processingSuccessful;
            TransportType = transportType;
        }
    }


    [ProtoContract]
    [InfrastructureMessage]
    public class ShadowCompletionMessage : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;
        [ProtoMember(2, IsRequired = true)]
        public readonly string FromPeer;
        [ProtoMember(3, IsRequired = true)]
        public readonly string ToPeer;
        [ProtoMember(4, IsRequired = true)]
        public readonly bool ProcessingSuccessful;
        [ProtoMember(5, IsRequired = true)]
        public readonly WireTransportType TransportType;

        public ShadowCompletionMessage(Guid messageId, string fromPeer, string toPeer, bool processingSuccessful, WireTransportType transportType)
        {
            MessageId = messageId;
            FromPeer = fromPeer;
            ToPeer = toPeer;
            ProcessingSuccessful = processingSuccessful;
            TransportType = transportType;
        }
    }
}