using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                return String.Equals(PeerName, (string) other.PeerName) && String.Equals(MessageType, (string) other.MessageType);
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
        private readonly Dictionary<StartUpKey, IStartupReliabilityStrategy> _strategies = new Dictionary<StartUpKey,IStartupReliabilityStrategy>();

        public StartupStrategyManager(IReliabilityStrategyFactory factory, IMessageOptionsRepository optionsRepository)
        {
            _factory = factory;
            _optionsRepository = optionsRepository;
        }

        public IEnumerable<IReceivedTransportMessage> CheckMessage(IReceivedTransportMessage transportMessage)
        {
            IStartupReliabilityStrategy strategy;
            var startUpKey = new StartUpKey(transportMessage.PeerName, transportMessage.MessageType);
            if(!_strategies.TryGetValue(startUpKey,out strategy ))
            {
                var optionsForMessageType = _optionsRepository.GetOptionsFor(transportMessage.MessageType);
                strategy = _factory.GetStartupStrategy(optionsForMessageType, transportMessage.PeerName,
                                            transportMessage.MessageType);
                _strategies.Add(startUpKey, strategy);
            }

            return strategy.GetMessagesToBubbleUp(transportMessage);
        }
    }
}