using System;
using ProtoBuf;
using Shared;

namespace ZmqServiceBus.Transport
{
    [ProtoContract]
    public class AcknowledgementMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;
        [ProtoMember(2, IsRequired = true)]
        public readonly bool ProcessingSuccessful;

        public AcknowledgementMessage(Guid messageId, bool processingSuccessful)
        {
            MessageId = messageId;
            ProcessingSuccessful = processingSuccessful;
        }
    }
}