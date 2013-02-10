using System;
using System.Collections.Generic;
using System.Linq;
using Bus.Attributes;
using Bus.BusEventProcessorCommands;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Shared;
using log4net;

namespace Bus.DisruptorEventHandlers
{
    class PersistenceSynchronizationProcessor : IEventHandler<InboundMessageProcessingEntry>
    {
        private bool _isInitialized = false;
        private readonly Dictionary<string, MessageSubscription> _options = new Dictionary<string, MessageSubscription>();
        private readonly Queue<InboundMessageProcessingEntry> _waitingMessages = new Queue<InboundMessageProcessingEntry>();
        private readonly Dictionary<string, bool> _infrastructureConditionCache = new Dictionary<string, bool>();
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof(PersistenceSynchronizationProcessor));
        private readonly IMessageSender _messageSender;
        private readonly ISequenceNumberVerifier _sequenceNumberVerifier;
        private readonly IPeerManager _peerManager;


        public PersistenceSynchronizationProcessor(IPeerConfiguration peerConfiguration, IMessageSender messageSender, ISequenceNumberVerifier sequenceNumberVerifier, IPeerManager peerManager)
        {
            _peerConfiguration = peerConfiguration;
            _messageSender = messageSender;
            _sequenceNumberVerifier = sequenceNumberVerifier;
            _peerManager = peerManager;
            _peerManager.PeerConnected += OnPeerConnected;
        }

        private void OnPeerConnected(ServicePeer obj)
        {
            if (obj.PeerName == _peerConfiguration.PeerName)
            {
                _options.Clear();
                foreach (var messageSubscription in obj.HandledMessages)
                {
                    _options[messageSubscription.MessageType.FullName] = messageSubscription;
                }
            }
        }


        public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
        {
            if (data.Command != null)
            {
                HandleCommand(data.Command, data);
                return;
            }

            var type = TypeUtils.Resolve(data.InitialTransportMessage.MessageType);
             MessageSubscription options;
            _options.TryGetValue(type.FullName, out options);

            if (data.ForceMessageThrough && _isInitialized)
                return; //sync message should be discarded

            if (!_sequenceNumberVerifier.IsSequenceNumberValid(data, _isInitialized))
                SetUninitializedAndSync();

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
                    {
                        _messageSender.Send(new StopSynchWithBrokerCommand(_peerConfiguration.PeerName));
                        _waitingMessages.Dequeue();
                        PublishMessageToStandardDispatch(deserializedMessage, data.InitialTransportMessage.MessageIdentity,
                                                         data.InitialTransportMessage.Endpoint, data.InitialTransportMessage.PeerName, data.InboundEntries);
                        ReleaseCachedMessages(data);
                        return;
                    }

                }
            }

            if (_isInitialized || options == null || options.ReliabilityLevel == ReliabilityLevel.FireAndForget || data.ForceMessageThrough)
                PublishMessageToStandardDispatch(deserializedMessage, data.InitialTransportMessage.MessageIdentity,
                                                         data.InitialTransportMessage.Endpoint, data.InitialTransportMessage.PeerName, data.InboundEntries);

            else
                _waitingMessages.Enqueue(data);
        }

        private void HandleCommand(IBusEventProcessorCommand command, InboundMessageProcessingEntry data)
        {
            if (command is ReleaseCachedMessages) // set initialized and publish buffered messages
            {
                //     ReleaseCachedMessages(data);
            }

            if (command is ResetSequenceNumbersForPeer)
            {
                var typedCommand = command as ResetSequenceNumbersForPeer;
                _logger.DebugFormat("Resetting sequence numbers for peer {0}", typedCommand.PeerName);
                _sequenceNumberVerifier.ResetSequenceNumbersForPeer(typedCommand.PeerName);

            }
        }

        private void ReleaseCachedMessages(InboundMessageProcessingEntry data)
        {
            var count = _waitingMessages.Count;
            _logger.DebugFormat("Releasing cached messages, count = {0}", count);
            _isInitialized = true;
            for (int i = 0; i < count; i++)
            {
                InboundMessageProcessingEntry item = _waitingMessages.Dequeue();
                if (!_sequenceNumberVerifier.IsSequenceNumberValid(item, _isInitialized))
                {
                    SetUninitializedAndSync();
                    return;
                }
                var itemType = TypeUtils.Resolve(item.InitialTransportMessage.MessageType);
                var deserializedSavedMessage = BusSerializer.Deserialize(item.InitialTransportMessage.Data, itemType) as IMessage;
                PublishMessageToStandardDispatch(deserializedSavedMessage, item.InitialTransportMessage.MessageIdentity,
                                                                           item.InitialTransportMessage.Endpoint, item.InitialTransportMessage.PeerName, data.InboundEntries);
            }
        }

        private void SetUninitializedAndSync()
        {
            _logger.Info("Re-syncing");
            _isInitialized = false;
            _messageSender.Send(new SynchronizeWithBrokerCommand(_peerConfiguration.PeerName)); //resync
        }

        private void PublishMessageToStandardDispatch(IMessage deserializedMessage, Guid messageId, IEndpoint endpoint, string peerName, List<InboundBusinessMessageEntry> entriesList)
        {
            var inboundEntry = new InboundBusinessMessageEntry();
            inboundEntry.DeserializedMessage = deserializedMessage;
            inboundEntry.MessageIdentity = messageId;
            inboundEntry.Endpoint = endpoint;
            inboundEntry.SendingPeer = peerName;
            entriesList.Add(inboundEntry);

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
