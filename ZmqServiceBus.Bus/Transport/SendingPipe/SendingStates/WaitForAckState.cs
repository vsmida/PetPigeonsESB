using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    internal class WaitForAckState : ISendingReliabilityStrategyState
    {
        public WaitForAckState(IEnumerable<Guid> relevantMessageIds)
        {
            RelevantMessageIds = new HashSet<Guid>(relevantMessageIds);
        }

        
        public IEnumerable<Guid> RelevantMessageIds { get; private set; }

        public bool CheckMessage(IReceivedTransportMessage message)
        {
            if (RelevantMessageIds.Contains(message.MessageIdentity) && message.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
            {
                WaitConditionFulfilled();
                return true;
            }
            return false;
        }

        public event Action WaitConditionFulfilled = delegate{};
    }
}