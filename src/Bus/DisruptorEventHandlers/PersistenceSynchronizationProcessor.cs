using System;
using System.Collections.Generic;
using System.Linq;
using Bus.Attributes;
using Bus.BusEventProcessorCommands;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Serializer;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Disruptor;
using Shared;
using StructureMap;
using log4net;
using IMessageSender = Bus.Transport.SendingPipe.IMessageSender;

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
        private readonly Dictionary<Type, IMessageSerializer> _typeToCustomSerializer = new Dictionary<Type, IMessageSerializer>();
        private readonly IContainer _objectFactory;


        public PersistenceSynchronizationProcessor(IPeerConfiguration peerConfiguration, IMessageSender messageSender, ISequenceNumberVerifier sequenceNumberVerifier, IPeerManager peerManager, IAssemblyScanner scanner, IContainer objectFactory)
        {
            _peerConfiguration = peerConfiguration;
            _messageSender = messageSender;
            _sequenceNumberVerifier = sequenceNumberVerifier;
            _peerManager = peerManager;
            _objectFactory = objectFactory;
            _peerManager.PeerConnected += OnPeerConnected;
            var serializers = scanner.FindMessageSerializers();
            foreach (var typeToSerializerType in serializers ?? new Dictionary<Type, Type>())
            {
                _typeToCustomSerializer.Add(typeToSerializerType.Key, _objectFactory.GetInstance(typeToSerializerType.Value) as IMessageSerializer);
            }
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

            if (!_sequenceNumberVerifier.IsSequenceNumberValid(data.InitialTransportMessage, _isInitialized))
                SetUninitializedAndSync();

            IMessageSerializer customSerializer = null;
            IMessage deserializedMessage;
            if (_typeToCustomSerializer.TryGetValue(type, out customSerializer))
                deserializedMessage = customSerializer.Deserialize(data.InitialTransportMessage.Data);
            else
                deserializedMessage = BusSerializer.Deserialize(data.InitialTransportMessage.Data, type) as IMessage;

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
                        PublishQueuedMessageToStandardDispatch(deserializedMessage, data.InitialTransportMessage.MessageIdentity,
                                                         data.InitialTransportMessage.Endpoint, data.InitialTransportMessage.PeerName, data);
                        ReleaseCachedMessages(data);
                        return;
                    }

                }
            }

            if (_isInitialized || options == null || options.ReliabilityLevel == ReliabilityLevel.FireAndForget || data.ForceMessageThrough)
                PublishToStandardDispatch(deserializedMessage, data.InitialTransportMessage.MessageIdentity,
                                                         data.InitialTransportMessage.Endpoint, data.InitialTransportMessage.PeerName, data);

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
                if (!_sequenceNumberVerifier.IsSequenceNumberValid(item.InitialTransportMessage, _isInitialized))
                {
                    SetUninitializedAndSync();
                    return;
                }
                var itemType = TypeUtils.Resolve(item.InitialTransportMessage.MessageType);

                IMessageSerializer customSerializer = null;
                IMessage deserializedSavedMessage;
                if (_typeToCustomSerializer.TryGetValue(itemType, out customSerializer))
                    deserializedSavedMessage = customSerializer.Deserialize(item.InitialTransportMessage.Data);
                else
                    deserializedSavedMessage = BusSerializer.Deserialize(item.InitialTransportMessage.Data, itemType) as IMessage;
                PublishQueuedMessageToStandardDispatch(deserializedSavedMessage, item.InitialTransportMessage.MessageIdentity,
                                                                           item.InitialTransportMessage.Endpoint, item.InitialTransportMessage.PeerName, data);
            }
        }

        private void SetUninitializedAndSync()
        {
            _logger.Info("Re-syncing");
            _isInitialized = false;
            _messageSender.Send(new SynchronizeWithBrokerCommand(_peerConfiguration.PeerName)); //resync
        }

        private void PublishQueuedMessageToStandardDispatch(IMessage deserializedMessage, Guid messageId, IEndpoint endpoint, string peerName, InboundMessageProcessingEntry data)
        {
            var inboundEntry = new InboundBusinessMessageEntry();
            inboundEntry.DeserializedMessage = deserializedMessage;
            inboundEntry.MessageIdentity = messageId;
            inboundEntry.Endpoint = endpoint;
            inboundEntry.SendingPeer = peerName;
            if (data.QueuedInboundEntries == null)
                data.QueuedInboundEntries = new List<InboundBusinessMessageEntry>();
            data.QueuedInboundEntries.Add(inboundEntry);
            data.IsStrandardMessage = true;

        }

        private void PublishToStandardDispatch(IMessage deserializedMessage, Guid messageId, IEndpoint endpoint, string peerName, InboundMessageProcessingEntry entry)
        {
            entry.InboundBusinessMessageEntry.DeserializedMessage = deserializedMessage;
            entry.InboundBusinessMessageEntry.MessageIdentity = messageId;
            entry.InboundBusinessMessageEntry.Endpoint = endpoint;
            entry.InboundBusinessMessageEntry.SendingPeer = peerName;
            entry.IsStrandardMessage = true;

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
            data.IsInfrastructureMessage = true;
            data.InfrastructureEntry = new InboundInfrastructureEntry();
            data.InfrastructureEntry.DeserializedMessage = deserializedMessage;
            data.InfrastructureEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
            data.InfrastructureEntry.Endpoint = data.InitialTransportMessage.Endpoint;
            data.InfrastructureEntry.SendingPeer = data.InitialTransportMessage.PeerName;
        }
    }
}
