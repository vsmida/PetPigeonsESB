using System;
using System.Threading;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    public interface ISendingReliabilityStrategyState
    {
        Guid SentMessageId { get; }
        bool CheckMessage(IReceivedTransportMessage message);
        WaitHandle WaitHandle { get; }
    }
}