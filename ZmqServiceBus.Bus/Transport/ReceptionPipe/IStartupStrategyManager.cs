using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PersistenceService.Commands;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface IStartupStrategyManager
    {
        IEnumerable<IReceivedTransportMessage> CheckMessage(Transport.IReceivedTransportMessage transportMessage);
    }

    public class StartupStrategyManager : IStartupStrategyManager
    {
        private class StartUpKey
        {
            public string PeerName { get; set; }
            public string MessageType { get; set; }

            public StartUpKey(string peerName, string messageType)
            {
                PeerName = peerName;
                MessageType = messageType;
            }

            protected bool Equals(StartUpKey other)
            {
                return String.Equals(PeerName, (string)other.PeerName) && String.Equals(MessageType, (string)other.MessageType);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((StartUpKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((PeerName != null ? PeerName.GetHashCode() : 0) * 397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
                }
            }
        }

        private readonly IReliabilityStrategyFactory _factory;
        private readonly IMessageOptionsRepository _optionsRepository;
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;
        private readonly Dictionary<StartUpKey, IStartupReliabilityStrategy> _strategies = new Dictionary<StartUpKey, IStartupReliabilityStrategy>();

        public StartupStrategyManager(IReliabilityStrategyFactory factory, IMessageOptionsRepository optionsRepository, IPersistenceSynchronizer persistenceSynchronizer)
        {
            _factory = factory;
            _optionsRepository = optionsRepository;
            _persistenceSynchronizer = persistenceSynchronizer;
        }

        public IEnumerable<IReceivedTransportMessage> CheckMessage(IReceivedTransportMessage transportMessage)
        {
            if (transportMessage.MessageType == typeof(ProcessMessagesCommand).FullName)
            {
                return HandleBrokerMessage(transportMessage);
            }
            
            var startUpKey = new StartUpKey(transportMessage.PeerName, transportMessage.MessageType);
            var strategy = GetStrategy(startUpKey);
            return strategy.GetMessagesToBubbleUp(transportMessage);
        }

        private IEnumerable<IReceivedTransportMessage> HandleBrokerMessage(IReceivedTransportMessage transportMessage)
        {
            var deserializedMessage = Serializer.Deserialize<ProcessMessagesCommand>(transportMessage.Data);
            var startUpKey = new StartUpKey(deserializedMessage.OriginatingPeer, deserializedMessage.MessageType);
            var strategy = GetStrategy(startUpKey);
            var result = deserializedMessage.MessagesToProcess.Aggregate(Enumerable.Empty<IReceivedTransportMessage>(),
                                                                         (current, receivedTransportMessage) => current.Concat(strategy.GetMessagesToBubbleUp(receivedTransportMessage)));
            if (deserializedMessage.IsEndOfQueue)
                result = result.Concat(strategy.SetEndOfBrokerQueue());
            return result;
        }

        private IStartupReliabilityStrategy GetStrategy(StartUpKey startUpKey)
        {
            IStartupReliabilityStrategy strategy;
            if (!_strategies.TryGetValue(startUpKey, out strategy))
            {
                var optionsForMessageType = _optionsRepository.GetOptionsFor(startUpKey.MessageType);
                strategy = _factory.GetStartupStrategy(optionsForMessageType, startUpKey.PeerName,
                                                       startUpKey.MessageType, _persistenceSynchronizer);
                _strategies.Add(startUpKey, strategy);
            }
            return strategy;
        }
    }
}