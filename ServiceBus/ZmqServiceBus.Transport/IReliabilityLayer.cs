using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;

namespace ZmqServiceBus.Transport
{
    public interface IReliabilityLayer : IDisposable
    {
        void RegisterMessageReliabilitySetting(Type messageType, MessageOptions level);
        void Send(ITransportMessage message);
        void Publish(ITransportMessage message);
        void Route(ITransportMessage message);
        event Action<ITransportMessage> OnMessageReceived;
        void Initialize();
    }

    public class ReliabilityLayer : IReliabilityLayer
    {
        private readonly ConcurrentDictionary<Guid, IReliabilityStrategy> _messageIdToReliabilityInfo = new ConcurrentDictionary<Guid, IReliabilityStrategy>();
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
        }

        private void CreateEventThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }
                                     }).Start();
        }

        private void OnEndpointManagerMessageReceived(ITransportMessage transportMessage)
        {
            if (transportMessage.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
            {
                IReliabilityStrategy strategy;
                if (_messageIdToReliabilityInfo.TryGetValue(transportMessage.MessageIdentity, out strategy))
                {
                    strategy.CheckMessage(transportMessage);
                }

            }

            if (transportMessage.MessageType == typeof(AcknowledgementMessage).FullName)
            {

            }
            _messagesToForward.Add(transportMessage);
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

        private void RegisterReliabilityStrategyAndForward(ITransportMessage message, Action<IReliabilityStrategy> forwardAction)
        {
            MessageOptions reliabilityLevel;
            if (_messageTypeToReliabilitySetting.TryGetValue(message.MessageType, out reliabilityLevel))
            {
                var messageStrategy = _reliabilityStrategyFactory.GetStrategy(reliabilityLevel);
                _messageIdToReliabilityInfo.TryAdd(message.MessageIdentity, messageStrategy);
                forwardAction(messageStrategy);
            }
        }

        public void Publish(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.PublishOn(_endpointManager, message));

        }

        public void Route(ITransportMessage message)
        {
            RegisterReliabilityStrategyAndForward(message, x => x.RouteOn(_endpointManager, message));
        }

        public void Dispose()
        {
            _running = false;
            _endpointManager.Dispose();
        }
    }
}