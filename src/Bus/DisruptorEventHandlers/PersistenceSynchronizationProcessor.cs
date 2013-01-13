using System;
using System.Collections.Generic;
using System.Linq;
using Bus.BusEventProcessorCommands;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Disruptor;
using Shared;
using Shared.Attributes;

namespace Bus.DisruptorEventHandlers
{
    class PersistenceSynchronizationProcessor : IEventHandler<InboundMessageProcessingEntry>
    {
        private RingBuffer<InboundInfrastructureEntry> _infrastructureBuffer;
        private RingBuffer<InboundBusinessMessageEntry> _standardMessagesBuffer;
        private volatile bool _isInitialized = false;
        private readonly IMessageOptionsRepository _optionsRepository;
        private readonly Dictionary<string, MessageOptions> _options = new Dictionary<string, MessageOptions>();
        private Queue<InboundMessageProcessingEntry> _waitingMessages = new Queue<InboundMessageProcessingEntry>();


        public PersistenceSynchronizationProcessor(IMessageOptionsRepository optionsRepository)
        {
            _optionsRepository = optionsRepository;
            _optionsRepository.OptionsUpdated += OnOptionsUpdated;
        }

        private void OnOptionsUpdated(MessageOptions option)
        {
            _options[option.MessageType] = option;
        }

        public void Initialize(RingBuffer<InboundInfrastructureEntry> infraBuffer, RingBuffer<InboundBusinessMessageEntry> standardBuffer)
        {
            _infrastructureBuffer = infraBuffer;
            _standardMessagesBuffer = standardBuffer;
        }

        public void OnNext(InboundMessageProcessingEntry data, long sequence, bool endOfBatch)
        {
            if(data.Command != null)
            {
                HandleCommand(data.Command);
                return;
            }

            var type = TypeUtils.Resolve(data.InitialTransportMessage.MessageType);
            MessageOptions options;
            _options.TryGetValue(type.FullName, out options);
            var deserializedMessage = BusSerializer.Deserialize(data.InitialTransportMessage.Data, type) as IMessage;

            if (IsInfrastructureMessage(type))
                PushIntoInfrastructureQueue(data, deserializedMessage);

            else
            {
                if (_isInitialized || options == null || options.ReliabilityLevel == ReliabilityLevel.FireAndForget || data.ForceMessageThrough)
                    PublishMessageToStandardDispatch(data, deserializedMessage);

                else
                    _waitingMessages.Enqueue(data);
            }
        }

        private void HandleCommand(IBusEventProcessorCommand command)
        {
            if (command is ReleaseCachedMessages) // set initialized and publish buffered messages
            {
                _isInitialized = true;
                for (int i = 0; i < _waitingMessages.Count; i++)
                {
                    var item = _waitingMessages.Dequeue();
                    var itemType = TypeUtils.Resolve(item.InitialTransportMessage.MessageType);
                    var deserializedSavedMessage = BusSerializer.Deserialize(item.InitialTransportMessage.Data, itemType) as IMessage;
                    PublishMessageToStandardDispatch(item, deserializedSavedMessage);
                }
            }
        }

        private void PublishMessageToStandardDispatch(InboundMessageProcessingEntry data, IMessage deserializedMessage)
        {
            var sequenceStandard = _standardMessagesBuffer.Next();
            var messageEntry = _standardMessagesBuffer[sequenceStandard];
            messageEntry.DeserializedMessage = deserializedMessage;
            messageEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
            messageEntry.SendingPeer = data.InitialTransportMessage.PeerName;
            messageEntry.TransportType = data.InitialTransportMessage.TransportType;
            _standardMessagesBuffer.Publish(sequenceStandard);
        }

        private static bool IsInfrastructureMessage(Type type)
        {
            return type.GetCustomAttributes(typeof(InfrastructureMessageAttribute), true).Any();
        }

        private void PushIntoInfrastructureQueue(InboundMessageProcessingEntry data, IMessage deserializedMessage)
        {
            var sequenceInfra = _infrastructureBuffer.Next();
            var messageEntry = _infrastructureBuffer[sequenceInfra];
            messageEntry.DeserializedMessage = deserializedMessage;
            messageEntry.ServiceInitialized = _isInitialized;
            messageEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
            messageEntry.SendingPeer = data.InitialTransportMessage.PeerName;
            messageEntry.TransportType = data.InitialTransportMessage.TransportType;
            _infrastructureBuffer.Publish(sequenceInfra);
        }
    }
}
