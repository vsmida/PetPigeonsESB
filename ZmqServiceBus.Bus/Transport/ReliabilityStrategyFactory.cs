using System;
using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;
        private readonly ConcurrentDictionary<string, ISendingReliabilityStrategy> _messageTypeToStrategies = new ConcurrentDictionary<string, ISendingReliabilityStrategy>();

        public ReliabilityStrategyFactory(IPersistenceSynchronizer persistenceSynchronizer)
        {
            _persistenceSynchronizer = persistenceSynchronizer;
        }

        public ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions)
        {
            switch (messageOptions.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForget();
                    break;
                    //case ReliabilityOption.SendToClientAndBrokerNoAck:
                    //    break;
                case ReliabilityLevel.Persisted:
                    ISendingReliabilityStrategy strategy;
                    if(!_messageTypeToStrategies.TryGetValue(messageOptions.MessageType, out strategy))
                    { //not threadsafe at all
                        strategy = new PersistStrategyCommands(_persistenceSynchronizer);
                        _messageTypeToStrategies.TryAdd(messageOptions.MessageType, strategy);
                    }
                    return strategy;
                    break;
                    //case ReliabilityOption.ClientAndBrokerReceivedOnTransport:
                    //    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}