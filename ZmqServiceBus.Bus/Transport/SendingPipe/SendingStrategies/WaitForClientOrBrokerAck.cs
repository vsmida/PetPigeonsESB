using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class WaitForClientOrBrokerAck : ISendingReliabilityStrategy
    {
        private readonly IEndpoint _brokerEndpoint;
        private readonly ISendingStrategyStateManager _stateManager;
        private readonly IDataSender _dataSender;

        //todo: special case when acknowledgement message. special message to broker to flush from queue? only for routing?
        public WaitForClientOrBrokerAck(IEndpoint brokerEndpoint, ISendingStrategyStateManager stateManager, IDataSender dataSender)
        {
            _brokerEndpoint = brokerEndpoint;
            _stateManager = stateManager;
            _dataSender = dataSender;
        }

        public void Send(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            SendingBusMessage brokerMessage;
            if (message.MessageType == typeof(CompletionAcknowledgementMessage).FullName)
                brokerMessage = new SendingBusMessage(typeof(ForgetMessageCommand).FullName, Guid.NewGuid(), Serializer.Serialize(new ForgetMessageCommand(message.MessageType, message.MessageIdentity)));
            else
                brokerMessage = new SendingBusMessage(typeof(PersistMessageCommand).FullName, message.MessageIdentity, Serializer.Serialize(new PersistMessageCommand(message)));

            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new WaitForAckState(message.MessageIdentity);

            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);

            _dataSender.SendMessage(brokerMessage, _brokerEndpoint);
            foreach (var endpoint in concernedSubscriptions.Select(x => x.Endpoint))
            {
                _dataSender.SendMessage(message, endpoint);
            }
            WaitHandle.WaitAny(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

        public void Publish(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            var brokerMessage = new SendingBusMessage(message.MessageType, message.MessageIdentity, Serializer.Serialize(new PersistMessageCommand(message)));

            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new PublishWaitForAckState(message.MessageIdentity, concernedSubscriptions.Select(x => x.Peer));

            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);

            _dataSender.SendMessage(brokerMessage, _brokerEndpoint);
            foreach (var endpoint in concernedSubscriptions.Select(x => x.Endpoint))
            {
                _dataSender.SendMessage(message, endpoint);
            }
            WaitHandle.WaitAny(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }
    }
}