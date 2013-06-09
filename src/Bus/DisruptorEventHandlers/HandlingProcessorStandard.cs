using System;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using System.Linq;
using log4net;

namespace Bus.DisruptorEventHandlers
{
    class HandlingProcessorStandard : IEventHandler<InboundMessageProcessingEntry>
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;
        private readonly ILog _logger = LogManager.GetLogger(typeof(HandlingProcessorStandard));


        public HandlingProcessorStandard(IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void OnNext(InboundMessageProcessingEntry inboundMessageProcessingEntry, long sequence, bool endOfBatch)
        {
            if (!inboundMessageProcessingEntry.IsStrandardMessage)
                return;
            var queuedMessages = inboundMessageProcessingEntry.QueuedInboundEntries;
            if (queuedMessages != null)
            {
                foreach (var entry in queuedMessages)
                {
                    HandleMessage(entry.DeserializedMessage, entry.SendingPeer, entry.Endpoint, entry.MessageIdentity);
                }
            }
            else
            {
                var entry = inboundMessageProcessingEntry.InboundBusinessMessageEntry;
                HandleMessage(entry.DeserializedMessage, entry.SendingPeer, entry.Endpoint, entry.MessageIdentity);
            }
        }

        private void HandleMessage(IMessage deserializedMessage, PeerId sendingPeer, IEndpoint endpoint, Guid messageId)
        {
            using (MessageContext.SetContext(sendingPeer, endpoint))
            {
                try
                {
                    _dispatcher.Dispatch(deserializedMessage);
                    if (!(deserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = deserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(messageId, messageType, true, sendingPeer, endpoint);
                    }
                }
                catch (Exception)
                {
                    if (!(deserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = deserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(messageId, messageType, false, sendingPeer, endpoint);
                    }
                }
            }
        }
    }
}