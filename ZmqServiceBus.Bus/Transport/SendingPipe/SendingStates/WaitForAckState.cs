using System;
using System.Threading;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    internal class WaitForAckState : ISendingReliabilityStrategyState
    {
        public WaitForAckState(Guid sentMessageId)
        {
            SentMessageId = sentMessageId;
        }

        public Guid SentMessageId { get; private set; }
        public WaitHandle WaitHandle { get { return _waitHandle; } }
        private AutoResetEvent _waitHandle;

        public bool CheckMessage(IReceivedTransportMessage message)
        {
            if (message.MessageIdentity == SentMessageId && message.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
            {
                _waitHandle.Set();
                return true;
            }
            return false;
        }

    }
}