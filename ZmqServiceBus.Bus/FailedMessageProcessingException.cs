using System;

namespace ZmqServiceBus.Bus
{
    public class FailedMessageProcessingException : Exception
    {
        public FailedMessageProcessingException(AcknowledgementMessage message)
            : base(string.Format("Message processing failed on message {0}", message.MessageId))
        {
        }
    }
}