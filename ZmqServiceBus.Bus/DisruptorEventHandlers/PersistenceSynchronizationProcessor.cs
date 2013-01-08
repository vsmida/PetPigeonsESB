using System;
using System.Collections.Generic;
using System.Linq;
using Disruptor;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.DisruptorEventHandlers
{
    public class PersistenceSynchronizationProcessor : IEventHandler<InboundMessageProcessingEntry>
    {
        private RingBuffer<InboundInfrastructureEntry> _infrastructureBuffer;
        private RingBuffer<InboundBusinessMessageEntry> _standardMessagesBuffer;
        private volatile bool _isInitialized = false;
        private int _numberOfBufferedMessages = 0;
        private readonly IMessageOptionsRepository _optionsRepository;
        private readonly Dictionary<string, MessageOptions> _options = new Dictionary<string, MessageOptions>();

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
            var type = TypeUtils.Resolve(data.InitialTransportMessage.MessageType);
            MessageOptions options;
            _options.TryGetValue(type.FullName, out options);
            var deserializedMessage = BusSerializer.Deserialize(data.InitialTransportMessage.Data, type) as IMessage;

            if (type == typeof(EndOfPersistedMessages)) // set initialized and publish buffered messages
            {
                _isInitialized = true;
                for (int i = 0; i < _numberOfBufferedMessages; i++)
                {
                    _standardMessagesBuffer.Publish(i);
                }
            }

            if (IsInfrastructureMessage(type))
            {
                PushIntoInfrastructureQueue(data, deserializedMessage);
            }
            else
            {
                var sequenceStandard = _standardMessagesBuffer.Next();
                var messageEntry = _standardMessagesBuffer[sequenceStandard];
                messageEntry.DeserializedMessage = deserializedMessage;
                messageEntry.MessageIdentity = data.InitialTransportMessage.MessageIdentity;
                messageEntry.SendingPeer = data.InitialTransportMessage.PeerName;
                messageEntry.TransportType = data.InitialTransportMessage.TransportType;
                if (_isInitialized || options == null || options.ReliabilityLevel == ReliabilityLevel.FireAndForget)
                    _standardMessagesBuffer.Publish(sequenceStandard);
                else
                    _numberOfBufferedMessages++;
            }
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
