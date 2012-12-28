using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{

 
    internal class WaitForClientOrBrokerAck : SendingReliabilityStrategy
    {
        private readonly ISendingStrategyStateManager _stateManager;
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;
        public override void SetupCommandReliabilitySafeguards(ISendingBusMessage message)
        {
            _persistenceSynchronizer.PersistMessage(message);

            var strategyState = new WaitForAckState(new[] { message.MessageIdentity });

            _stateManager.RegisterStrategy(strategyState);
            strategyState.WaitConditionFulfilled += ReliabilityAchieved;
        }

        public override void SetupEventReliabilitySafeguards(ISendingBusMessage message)
        {
            _persistenceSynchronizer.PersistMessage(message);

            var strategyState = new WaitForAckState(new[] { message.MessageIdentity });

            _stateManager.RegisterStrategy(strategyState);
            strategyState.WaitConditionFulfilled += ReliabilityAchieved;
        }

        public override event Action ReliabilityAchieved = delegate{};

        //todo: special case when acknowledgement message. special message to broker to flush from queue? only for routing?
        public WaitForClientOrBrokerAck(ISendingStrategyStateManager stateManager)
        {
            _stateManager = stateManager;
        }

    }
}