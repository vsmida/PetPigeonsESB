using System;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public class ReliabilityStrategyFactory : IReliabilityStrategyFactory
    {

        private readonly ISendingStrategyStateManager _stateManager;
        private readonly IDataSender _dataSender;


        public ReliabilityStrategyFactory(ISendingStrategyStateManager stateManager, IDataSender dataSender)
        {
            _stateManager = stateManager;
            _dataSender = dataSender;
        }

        public ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions)
        {
            switch (messageOptions.ReliabilityInfo.ReliabilityLevel)
            {
                case ReliabilityLevel.FireAndForget:
                    return new FireAndForget(_dataSender);
                    break;
                    //case ReliabilityOption.SendToClientAndBrokerNoAck:
                    //    break;
                case ReliabilityLevel.SomeoneReceivedMessageOnTransport:
                    return new WaitForClientOrBrokerAck(messageOptions.ReliabilityInfo.BrokerEndpoint, _stateManager, _dataSender);
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

        public void Dispose()
        {
            _dataSender.Dispose();
        }
    }
}