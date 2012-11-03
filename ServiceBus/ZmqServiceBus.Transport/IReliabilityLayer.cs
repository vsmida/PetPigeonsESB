using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZmqServiceBus.Transport
{
    public interface IReliabilityLayer
    {
        void RegisterMessageReliabilitySetting<T>(ReliabilityOption option);
        void Send(ITransportMessage message);
    }

    public class ReliabilityLayer : IReliabilityLayer
    {
        private readonly ConcurrentDictionary<Guid, IReliabilityStrategy> _messageIdToReliabilityInfo = new ConcurrentDictionary<Guid, IReliabilityStrategy>();
        private readonly ConcurrentDictionary<Guid, IReliabilityStrategy> _brokerSaveRequestIdToReliabilityInfo = new ConcurrentDictionary<Guid, IReliabilityStrategy>();
        private readonly Dictionary<string, ReliabilityOption> _messageTypeToReliabilitySetting = new Dictionary<string, ReliabilityOption>();
        private readonly IReliabilityStrategyFactory _reliabilityStrategyFactory;
        private readonly ITransport _transport;

        public ReliabilityLayer(IReliabilityStrategyFactory reliabilityStrategyFactory, ITransport transport)
        {
            _reliabilityStrategyFactory = reliabilityStrategyFactory;
            _transport = transport;
            _transport.OnMessageReceived += OnTransportMessageReceived;
        }

        private void OnTransportMessageReceived(ITransportMessage transportMessage)
        {
            if(transportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
            {
                IReliabilityStrategy strategy;
                if (_messageIdToReliabilityInfo.TryGetValue(transportMessage.MessageIdentity, out strategy))
                    strategy.ClientTransportAckReceived = true;
                else if (_brokerSaveRequestIdToReliabilityInfo.TryGetValue(transportMessage.MessageIdentity, out strategy))
                    strategy.BrokerTransportAckReceived = true;

            }

            if (transportMessage.MessageType == typeof(AcknowledgementMessage).FullName)
            {

            }

        }

        public void RegisterMessageReliabilitySetting<T>(ReliabilityOption option)
        {
            _messageTypeToReliabilitySetting[typeof(T).FullName]= option;
        }

        public void Send(ITransportMessage message)
        {
            ReliabilityOption reliabilityOption;
            if(_messageTypeToReliabilitySetting.TryGetValue(message.MessageType, out reliabilityOption))
            {
                var messageStrategy = _reliabilityStrategyFactory.GetStrategy(reliabilityOption);
                _messageIdToReliabilityInfo.TryAdd(message.MessageIdentity, messageStrategy);
                messageStrategy.WaitForReliabilityConditionsToBeFulfilled.WaitOne();
            }
        }
    }
}