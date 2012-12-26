using System;
using System.Collections.Generic;
using System.Threading;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    public interface ISendingReliabilityStrategyState
    {
        IEnumerable<Guid> RelevantMessageIds { get; }
        bool CheckMessage(IReceivedTransportMessage message);
        event Action WaitConditionFulfilled;
    }
}