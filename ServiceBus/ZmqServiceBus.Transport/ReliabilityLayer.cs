using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DirectoryService.Commands;
using Shared;

namespace ZmqServiceBus.Transport
{
    public class ReliabilityLayer : IReliabilityLayer
    {
        private readonly ConcurrentDictionary<Guid, ISendingReliabilityStrategy> _messageIdToReliabilityInfo = new ConcurrentDictionary<Guid, ISendingReliabilityStrategy>();
        private readonly Dictionary<StartUpKey, IStartupReliabilityStrategy> _startupKeyToStartupStrategy = new Dictionary<StartUpKey, IStartupReliabilityStrategy>();
        private readonly Dictionary<string, MessageOptions> _messageTypeToReliabilitySetting = new Dictionary<string, MessageOptions>();
        private readonly BlockingCollection<ITransportMessage> _messagesToForward = new BlockingCollection<ITransportMessage>();
        private readonly IReliabilityStrategyFactory _reliabilityStrategyFactory;
        private readonly IEndpointManager _endpointManager;
        public event Action<ITransportMessage> OnMessageReceived = delegate { };
        public void Initialize()
        {
            _endpointManager.Initialize();
        }

        private volatile bool _running = true;


        public ReliabilityLayer(IReliabilityStrategyFactory reliabilityStrategyFactory, IEndpointManager endpointManager)
        {
            _reliabilityStrategyFactory = reliabilityStrategyFactory;
            _endpointManager = endpointManager;
            _endpointManager.OnMessageReceived += OnEndpointManagerMessageReceived;
            CreateEventThread();
            RegisterMessageReliabilitySetting<ReceivedOnTransportAcknowledgement>(new MessageOptions(ReliabilityLevel.FireAndForget, null));
            RegisterMessageReliabilitySetting<InitializeTopologyAndMessageSettings>(new MessageOptions(ReliabilityLevel.FireAndForget, null));
            RegisterMessageReliabilitySetting<RegisterPeerCommand>(new MessageOptions(ReliabilityLevel.FireAndForget, null));
        }

        private void CreateEventThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromSeconds(1)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                     }).Start();
        }

        private void OnEndpointManagerMessageReceived(ITransportMessage transportMessage)
        {
            ReleaseSendingStrategy(transportMessage);

            ForwardMessagesAcceptedByStartupStrategy(transportMessage);
        }

        private void ForwardMessagesAcceptedByStartupStrategy(ITransportMessage transportMessage)
        {
            IStartupReliabilityStrategy startupStrategy;
            var startUpKey = new StartUpKey(transportMessage.PeerName, transportMessage.MessageType);
            if (!_startupKeyToStartupStrategy.TryGetValue(startUpKey, out startupStrategy))
            {
                startupStrategy =
                    _reliabilityStrategyFactory.GetStartupStrategy(_messageTypeToReliabilitySetting[transportMessage.MessageType], startUpKey.PeerName, startUpKey.MessageType);_startupKeyToStartupStrategy.Add(startUpKey, startupStrategy);
            }

            foreach (var message in startupStrategy.GetMessagesToBubbleUp(transportMessage))
            {
                _messagesToForward.Add(message);
            }
        }

        private void ReleaseSendingStrategy(ITransportMessage transportMessage)
        {
            ISendingReliabilityStrategy strategy;
            if (_messageIdToReliabilityInfo.TryGetValue(transportMessage.MessageIdentity, out strategy))
            {
                strategy.CheckMessage(transportMessage);
            }

            if (transportMessage.MessageType == typeof(AcknowledgementMessage).FullName)
            {
                //do stuff
            }
        }

        public void RegisterMessageReliabilitySetting<T>(MessageOptions level)
        {
            RegisterMessageReliabilitySetting(typeof(T), level);
        }

        public void RegisterMessageReliabilitySetting(Type messageType, MessageOptions level)
        {
            _messageTypeToReliabilitySetting[messageType.FullName] = level;
        }

        public void Send(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.SendOn(_endpointManager, message));
        }

        public void Publish(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.PublishOn(_endpointManager, message));
        }

        public void Route(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.RouteOn(_endpointManager, message));
        }

        private void RegisterReliabilityStrategyAndForward(ITransportMessage message, Action<ISendingReliabilityStrategy> forwardAction)
        {
            MessageOptions reliabilityLevel;
            if (_messageTypeToReliabilitySetting.TryGetValue(message.MessageType, out reliabilityLevel))
            {
                var messageStrategy = _reliabilityStrategyFactory.GetSendingStrategy(reliabilityLevel);
                _messageIdToReliabilityInfo.TryAdd(message.MessageIdentity, messageStrategy);
                forwardAction(messageStrategy);
            }
            else
                throw new ArgumentException("Message type is unknown");
        }

        public void Dispose()
        {
            _running = false;
            _endpointManager.Dispose();
        }

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
                return string.Equals(PeerName, other.PeerName) && string.Equals(MessageType, other.MessageType);
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
    }
}