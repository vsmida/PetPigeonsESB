using System;
using Bus.Attributes;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.InfrastructureMessages
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
        public readonly IEndpoint Endpoint;

        public CompletionAcknowledgementMessage(Guid messageId, string messageType, bool processingSuccessful, IEndpoint endpoint)
        {
            MessageId = messageId;
            ProcessingSuccessful = processingSuccessful;
            Endpoint = endpoint;
            MessageType = messageType;
        }
    }
}