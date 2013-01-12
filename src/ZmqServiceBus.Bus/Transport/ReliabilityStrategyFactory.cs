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
        private readonly ConcurrentDictionary<string, ISendingReliabilityStrategy> _messageTypeToStrategies = new ConcurrentDictionary<string, ISendingReliabilityStrategy>();

        public ReliabilityStrategyFactory()
        {
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
                    break;
                    //case ReliabilityOption.ClientAndBrokerReceivedOnTransport:
                    //    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
            return null;
        }
    }
}