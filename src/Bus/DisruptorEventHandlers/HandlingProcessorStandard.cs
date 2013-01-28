using System;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;

namespace Bus.DisruptorEventHandlers
{
    public class HandlingProcessorStandard : IEventHandler<InboundMessageProcessingEntry>
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;

        public HandlingProcessorStandard(IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void OnNext(InboundMessageProcessingEntry inboundMessageProcessingEntry, long sequence, bool endOfBatch)
        {
            var data = inboundMessageProcessingEntry.InboundEntry;
            if (data == null)
                return;
            HandleMessage(data.DeserializedMessage, data.SendingPeer, data.Endpoint, data.MessageIdentity);
        }

        private void HandleMessage(IMessage deserializedMessage, string sendingPeer, IEndpoint endpoint, Guid messageId)
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