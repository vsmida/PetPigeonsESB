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

        public WaitForClientOrBrokerAck(string brokerPeerName, ISendingStrategyStateManager stateManager)
        {
            _brokerPeerName = brokerPeerName;
            _stateManager = stateManager;
        }

        public void SendOn(IEndpointManager endpointManager, ISendingTransportMessage message)
        {
            var brokerMessage = new SendingTransportMessage(typeof(PersistMessageCommand).FullName, message.MessageIdentity, Serializer.Serialize(message));
            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new WaitForAckState(message.MessageIdentity);
            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);
            endpointManager.RouteMessage(brokerMessage, _brokerPeerName);
            endpointManager.SendMessage(message);
            WaitHandle.WaitAll(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

        public void PublishOn(IEndpointManager endpointManager, ISendingTransportMessage message)
        {
            var brokerMessage = new SendingTransportMessage(typeof(PersistMessageCommand).FullName, message.MessageIdentity, Serializer.Serialize(message));
            var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
            var strategyStateMessage = new WaitForAckState(message.MessageIdentity);
            _stateManager.RegisterStrategy(strategyStateBroker);
            _stateManager.RegisterStrategy(strategyStateMessage);
            endpointManager.RouteMessage(brokerMessage, _brokerPeerName);
            endpointManager.PublishMessage(message);
            WaitHandle.WaitAll(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }

        public void RouteOn(IEndpointManager endpointManager, ISendingTransportMessage message, string destinationPeer)
        {
            var brokerMessage = new SendingTransportMessage(typeof(PersistMessageCommand).FullName, message.MessageIdentity, Serializer.Serialize(message));
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