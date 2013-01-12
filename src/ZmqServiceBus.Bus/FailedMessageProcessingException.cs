using System;
using ZmqServiceBus.Bus.InfrastructureMessages;

namespace ZmqServiceBus.Bus
{
    public class FailedMessageProcessingException : Exception
    {
        public FailedMessageProcessingException(CompletionAcknowledgementMessage message)
            : base(string.Format("Message processing failed on message {0}", message.MessageId))
        {
        }
    }
}