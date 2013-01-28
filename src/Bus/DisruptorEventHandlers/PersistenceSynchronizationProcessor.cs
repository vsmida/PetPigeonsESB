using System;
using System.Collections.Generic;
using System.Linq;
using Bus.BusEventProcessorCommands;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Shared;
using Shared.Attributes;
using log4net;

namespace Bus.DisruptorEventHandlers
{
    class PersistenceSynchronizationProcessor : IEventHandler<InboundMessageProcessingEntry>
    {
        private bool _isInitialized = false;
        private readonly IMessageOptionsRepository _optionsRepository;
        private readonly Dictionary<string, MessageOptions> _options = new Dictionary<string, MessageOptions>();
        private readonly Queue<InboundMessageProcessingEntry> _waitingMessages = new Queue<InboundMessageProcessingEntry>();
        private readonly Dictionary<string, bool> _infrastructureConditionCache = new Dictionary<string, bool>();
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof(PersistenceSynchronizationProcessor));
        private readonly IMessageSender _messageSender;
        private readonly ISequenceNumberVerifier _sequenceNumberVerifier;

        public PersistenceSynchronizationProcessor(IMessageOptionsRepository optionsRepository, IPeerConfiguration peerConfiguration, IMessageSender messageSender, ISequenceNumberVerifier sequenceNumberVerifier)
        {
            _optionsRepository = optionsRepository;
            _peerConfiguration = peerConfiguration;
            _messageSender = messageSender;
            _sequenceNumberVerifier = sequenceNumberVerifier;
            _optionsRepository.OptionsUpdated += OnOptionsUpdated;
        }

        private void OnOptionsUpdated(MessageOptions option)
        {
            _options[option.MessageType] = option;
        }

        public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
        {
            if (data.Command != null)
            {
                HandleCommand(data.Command);
                return;
            }

            var type = TypeUtils.Resolve(data.InitialTransportMessage.MessageType);
            MessageOptions options;
            _options.TryGetValue(type.FullName, out options);

            if (!_sequenceNumberVerifier.IsSequenceNumberValid(data, _isInitialized))
                _isInitialized = false;

            var deserializedMessage = BusSerializer.Deserialize(data.InitialTransportMessage.Data, type) as IMessage;

            if (IsInfrastructureMessage(type))
            {
                PushIntoInfrastructureQueue(data, deserializedMessage);
                return;
            }

            if (data.ForceMessageThrough)
            {
                _logger.DebugFormat("Forcing message type {0} from {1} with seqNum = {2}", data.InitialTransportMessage.MessageType, data.InitialTransportMessage.PeerName, data.InitialTransportMessage.SequenceNumber);
                if (_waitingMessages.Count != 0)
                {
                    var message = _waitingMessages.Peek();
                    if (message.InitialTransportMessage.MessageIdentity == data.InitialTransportMessage.MessageIdentity)
                        _messageSender.Send(new StopSynchWithBrokerCommand(_peerConfiguration.PeerName));
                    _waitingMessages.Dequeue();
                }
            }

            if (_isInitialized || options == null || options.ReliabilityLevel == ReliabilityLevel.FireAndForget || data.ForceMessageThrough)
                PublishMessageToStandardDispatch(data, deserializedMessage);

            else
                _waitingMessages.Enqueue(data);
        }

        private void HandleCommand(IBusEventProcessorCommand command)
        {
            if (command is ReleaseCachedMessages) // set initialized and publish buffered messages
            {
                _logger.DebugFormat("Releasing cached messages, count = {0}", _waitingMessages.Count);
                _isInitialized = true;
                for (int i = 0; i < _waitingMessages.Count; i++)
                {
                    InboundMessageProcessingEntry item = _waitingMessages.Dequeue();
                    if (!_sequenceNumberVerifier.IsSequenceNumberValid(item, _isInitialized))
                        _isInitialized = false;
                    var itemType = TypeUtils.Resolve(item.InitialTransportMessage.MessageType);
                    var deserializedSavedMessage = BusSerializer.Deserialize(item.InitialTransportMessage.Data, itemType) as IMessage;
                    PublishMessageToStandardDispatch(item, deserializedSavedMessage);
                }
            }

            if (command is ResetSequenceNumbersForPeer)
            {
                var typedCommand = command as ResetSequenceNumbersForPeer;
                _logger.DebugFormat("Resetting sequence numbers for peer {0}", typedCommand.PeerName);
                _sequenceNumberVerifier.ResetSequenceNumbersForPeer(typedCommand.PeerName);

            }
        }

        private void PublishMessageToStandardDispatch(InboundMessageProcessingEntry data, IMessage deserializedMessage)
        {
            data.InboundEntry = new InboundBusinessMessageEntry();
            data.InboundEntry.DeserializedMessage = deserializedMessage;
            data.InboundEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
            data.InboundEntry.Endpoint = data.InitialTransportMessage.Endpoint;
            data.InboundEntry.SendingPeer = data.InitialTransportMessage.PeerName;

        }

        private bool IsInfrastructureMessage(Type type)
        {
            bool result;
            if (!_infrastructureConditionCache.TryGetValue(type.FullName, out result))
            {
                result = type.GetCustomAttributes(typeof(InfrastructureMessageAttribute), true).Any();
                _infrastructureConditionCache.Add(type.FullName, result);
            }
            return result;
        }

        private void PushIntoInfrastructureQueue(InboundMessageProcessingEntry data, IMessage deserializedMessage)
        {
            data.InfrastructureEntry = new InboundInfrastructureEntry();
            data.InfrastructureEntry.DeserializedMessage = deserializedMessage;
            data.InfrastructureEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
            data.InfrastructureEntry.Endpoint = data.InitialTransportMessage.Endpoint;
            data.InfrastructureEntry.SendingPeer = data.InitialTransportMessage.PeerName;
        }
    }
}
