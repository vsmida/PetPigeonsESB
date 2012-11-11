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
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();
        private readonly IReliabilityStrategyFactory _reliabilityStrategyFactory;
        private readonly IEndpointManager _endpointManager;
        public event Action<IReceivedTransportMessage> OnMessageReceived = delegate { };
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
                                             IReceivedTransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromSeconds(1)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                     }).Start();
        }

        private void OnEndpointManagerMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            ReleaseSendingStrategy(receivedTransportMessage);

            if (IsTransportAck(receivedTransportMessage))
                return;
            ForwardMessagesAcceptedByStartupStrategy(receivedTransportMessage);
        }

        private static bool IsTransportAck(IReceivedTransportMessage receivedTransportMessage)
        {
            return receivedTransportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName;
        }

        private void ForwardMessagesAcceptedByStartupStrategy(IReceivedTransportMessage receivedTransportMessage)
        {
            IStartupReliabilityStrategy startupStrategy;
            var startUpKey = new StartUpKey(receivedTransportMessage.PeerName, receivedTransportMessage.MessageType);
            if (!_startupKeyToStartupStrategy.TryGetValue(startUpKey, out startupStrategy))
            {
                startupStrategy =
                    _reliabilityStrategyFactory.GetStartupStrategy(_messageTypeToReliabilitySetting[receivedTransportMessage.MessageType], startUpKey.PeerName, startUpKey.MessageType); _startupKeyToStartupStrategy.Add(startUpKey, startupStrategy);
            }

            foreach (var message in startupStrategy.GetMessagesToBubbleUp(receivedTransportMessage))
            {
                _messagesToForward.Add(message);
            }
        }

        private void ReleaseSendingStrategy(IReceivedTransportMessage receivedTransportMessage)
        {
            ISendingReliabilityStrategy strategy = _sendingStrategyManager.GetSendingStrategy(receivedTransportMessage);
            if (strategy != null)
                strategy.CheckMessage(receivedTransportMessage);
        }

        private SendingStrategyKey GetSendingStrategyKey(IReceivedTransportMessage receivedTransportMessage)
        {
            if (IsTransportAck(receivedTransportMessage))
                return new SendingStrategyKey(receivedTransportMessage.MessageIdentity);
            if (receivedTransportMessage.MessageType == typeof(AcknowledgementMessage).FullName)
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

        public void Send(ISendingTransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.SendOn(_endpointManager, _sendingStrategyManager, message));
        }

        public void Publish(ISendingTransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.PublishOn(_endpointManager, _sendingStrategyManager, message));
        }

        public void Route(ISendingTransportMessage message, string destinationPeer)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.RouteOn(_endpointManager, _sendingStrategyManager, message, destinationPeer));
        }

        private void RegisterReliabilityStrategyAndForward(ISendingTransportMessage message, Action<ISendingReliabilityStrategy> forwardAction)
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