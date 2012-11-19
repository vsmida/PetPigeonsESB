using System;
using System.Collections.Concurrent;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    public class SendingStrategyStateManager : ISendingStrategyStateManager
    {
        private readonly ConcurrentDictionary<Guid, ISendingReliabilityStrategyState> _reliabilityStrategies = new ConcurrentDictionary<Guid, ISendingReliabilityStrategyState>();

        public void CheckMessage(IReceivedTransportMessage transportMessage)
        {
            foreach (var sendingReliabilityStrategy in _reliabilityStrategies)
            {
                if (sendingReliabilityStrategy.Value.CheckMessage(transportMessage))
                {
                    ISendingReliabilityStrategyState state;
                    _reliabilityStrategies.TryRemove(sendingReliabilityStrategy.Key, out state);
                }
            }
        }

        public void RegisterStrategy(ISendingReliabilityStrategyState state)
        {
            _reliabilityStrategies.TryAdd(state.SentMessageId, state);
        }
    }
}