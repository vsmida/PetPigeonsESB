using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    internal class PublishWaitForAckState : ISendingReliabilityStrategyState
    {
        private readonly List<string> _peersThatNeedToReply;

        public PublishWaitForAckState(Guid sentMessageId, IEnumerable<string> peersThatNeedToReply)
        {
            _peersThatNeedToReply = peersThatNeedToReply.ToList();
            SentMessageId = sentMessageId;
        }

        public Guid SentMessageId { get; private set; }
        public WaitHandle WaitHandle { get { return _waitHandle; } }
        private AutoResetEvent _waitHandle;

        public bool CheckMessage(IReceivedTransportMessage message)
        {
            if (message.MessageIdentity == SentMessageId && message.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
            {
                _peersThatNeedToReply.Remove(message.PeerName);
                if (_peersThatNeedToReply.Count == 0)
                {
                    _waitHandle.Set();
                    return true;
                }

            }
            return false;
        }

    }
}