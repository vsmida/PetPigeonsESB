using System;
using Shared;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {

        private readonly ISendingStrategyStateManager _stateManager;

        public ReliabilityStrategyFactory(ISendingStrategyStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions)
        {
            switch (messageOptions.ReliabilityInfo.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForget();
                    break;
                    //case ReliabilityOption.SendToClientAndBrokerNoAck:
                    //    break;
                case ReliabilityLevel.SomeoneReceivedMessageOnTransport:
                    return new WaitForClientOrBrokerAck(messageOptions.ReliabilityInfo.BrokerName, _stateManager);
                    break;
                    //case ReliabilityOption.ClientAndBrokerReceivedOnTransport:
                    //    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }

        public IStartupReliabilityStrategy GetStartupStrategy(MessageOptions messageOptions, string peerName, string messageType, IPersistenceSynchronizer synchronizer)
        {
            switch (messageOptions.ReliabilityInfo.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForgetStartupStrategy(null, null);
                case ReliabilityLevel.SendToClientAndBrokerNoAck:
                case ReliabilityLevel.SomeoneReceivedMessageOnTransport:
                case ReliabilityLevel.ClientAndBrokerReceivedOnTransport:
                    return new SynchronizeWithBrokerStartupStrategy(peerName, messageType, synchronizer);
                default:
                    throw new ArgumentOutOfRangeException("messageOptions");
            }
        }
    }
}