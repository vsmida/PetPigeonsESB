using System;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class WaitForClientOrBrokerAck : ISendingReliabilityStrategy
    {
        private readonly string _brokerPeerName;
        private readonly ISendingStrategyStateManager _stateManager;

        //todo: special case when acknowledgement message. special message to broker to flush from queue? only for routing?
        public WaitForClientOrBrokerAck(string brokerPeerName, ISendingStrategyStateManager stateManager)
        {
            _brokerPeerName = brokerPeerName;
            _stateManager = stateManager;
        }

        public void SendOn(IEndpointManager endpointManager, ISendingBusMessage message)
        {
            var brokerMessage = new SendingBusMessage(typeof(PersistMessageCommand).FullName, Guid.NewGuid(), Serializer.Serialize(message));
            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new WaitForAckState(message.MessageIdentity);
            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);
            endpointManager.RouteMessage(brokerMessage, _brokerPeerName);
            endpointManager.SendMessage(message);
            WaitHandle.WaitAll(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

        public void PublishOn(IEndpointManager endpointManager, ISendingBusMessage message)
        {
            var brokerMessage = new SendingBusMessage(typeof(PersistMessageCommand).FullName, Guid.NewGuid(), Serializer.Serialize(message));
            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new WaitForAckState(message.MessageIdentity);
            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);
            endpointManager.RouteMessage(brokerMessage, _brokerPeerName);
            endpointManager.PublishMessage(message);
            WaitHandle.WaitAll(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

        public void RouteOn(IEndpointManager endpointManager, ISendingBusMessage message, string destinationPeer)
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
            endpointManager.RouteMessage(brokerMessage, _brokerPeerName);
            endpointManager.RouteMessage(message, destinationPeer);
            WaitHandle.WaitAll(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

    }
}