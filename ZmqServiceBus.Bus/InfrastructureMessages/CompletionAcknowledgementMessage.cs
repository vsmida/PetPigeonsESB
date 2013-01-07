using System;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;

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

        public CompletionAcknowledgementMessage(Guid messageId, bool processingSuccessful)
        {
            MessageId = messageId;
            ProcessingSuccessful = processingSuccessful;
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

        public ShadowCompletionMessage(Guid messageId, string fromPeer, string toPeer, bool processingSuccessful)
        {
            MessageId = messageId;
            FromPeer = fromPeer;
            ToPeer = toPeer;
            ProcessingSuccessful = processingSuccessful;
        }
    }
}