using System;
using Bus.InfrastructureMessages;

namespace Bus
{
    public class FailedMessageProcessingException : Exception
    {
        public FailedMessageProcessingException(CompletionAcknowledgementMessage message)
            : base(string.Format("Message processing failed on message {0}", message.MessageId))
        {
        }
    }
}