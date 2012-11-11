using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public class ReliabilityLayer : IReliabilityLayer
    {
        private readonly ISendingStrategyManager _sendingStrategyManager;
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


        public ReliabilityLayer(IReliabilityStrategyFactory reliabilityStrategyFactory, IEndpointManager endpointManager, ISendingStrategyManager sendingStrategyManager)
        {
            _reliabilityStrategyFactory = reliabilityStrategyFactory;
            _endpointManager = endpointManager;
            _sendingStrategyManager = sendingStrategyManager;
            _endpointManager.OnMessageReceived += OnEndpointManagerMessageReceived;
            CreateEventThread();
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

            if (IsTransportAck(transportMessage))
                return;
            ForwardMessagesAcceptedByStartupStrategy(transportMessage);
        }

        private static bool IsTransportAck(ITransportMessage transportMessage)
        {
            return transportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName;
        }

        private void ForwardMessagesAcceptedByStartupStrategy(ITransportMessage transportMessage)
        {
            IStartupReliabilityStrategy startupStrategy;
            var startUpKey = new StartUpKey(transportMessage.PeerName, transportMessage.MessageType);
            if (!_startupKeyToStartupStrategy.TryGetValue(startUpKey, out startupStrategy))
            {
                startupStrategy =
                    _reliabilityStrategyFactory.GetStartupStrategy(_messageTypeToReliabilitySetting[transportMessage.MessageType], startUpKey.PeerName, startUpKey.MessageType); _startupKeyToStartupStrategy.Add(startUpKey, startupStrategy);
            }

            foreach (var message in startupStrategy.GetMessagesToBubbleUp(transportMessage))
            {
                _messagesToForward.Add(message);
            }
        }

        private void ReleaseSendingStrategy(ITransportMessage transportMessage)
        {
            ISendingReliabilityStrategy strategy = _sendingStrategyManager.GetSendingStrategy(transportMessage);
            if (strategy != null)
                strategy.CheckMessage(transportMessage);
        }

        private SendingStrategyKey GetSendingStrategyKey(ITransportMessage transportMessage)
        {
            if (IsTransportAck(transportMessage))
                return new SendingStrategyKey(transportMessage.MessageIdentity);
            if (transportMessage.MessageType == typeof(AcknowledgementMessage).FullName)
            {
                //do stuff
            }
            return null;
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
            RegisterReliabilityStrategyAndForward(message, x => x.SendOn(_endpointManager, _sendingStrategyManager, message));
        }

        public void Publish(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.PublishOn(_endpointManager, _sendingStrategyManager, message));
        }

        public void Route(ITransportMessage message, string destinationPeer)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.RouteOn(_endpointManager, _sendingStrategyManager, message, destinationPeer));
        }

        private void RegisterReliabilityStrategyAndForward(ITransportMessage message, Action<ISendingReliabilityStrategy> forwardAction)
        {
            MessageOptions reliabilityLevel;
            if (_messageTypeToReliabilitySetting.TryGetValue(message.MessageType, out reliabilityLevel))
            {
                var messageStrategy = _reliabilityStrategyFactory.GetSendingStrategy(reliabilityLevel);
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