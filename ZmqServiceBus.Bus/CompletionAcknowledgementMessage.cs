using System;
using ProtoBuf;
using Shared.Attributes;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
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
}