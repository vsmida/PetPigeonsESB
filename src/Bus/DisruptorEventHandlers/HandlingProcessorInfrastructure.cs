using System;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;

namespace Bus.DisruptorEventHandlers
{
    class HandlingProcessorInfrastructure : IEventHandler<InboundMessageProcessingEntry>
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;

        public HandlingProcessorInfrastructure(IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void OnNext(InboundMessageProcessingEntry messageProcessingEntry, long sequence, bool endOfBatch)
        {
            if(!messageProcessingEntry.IsInfrastructureMessage)
                return;
            var data = messageProcessingEntry.InfrastructureEntry;

            using (MessageContext.SetContext(data.SendingPeer, data.Endpoint))
            {
                try
                {
                    _dispatcher.Dispatch(data.DeserializedMessage);
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = data.DeserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(data.MessageIdentity, messageType, true, data.SendingPeer, data.Endpoint);
                    }
                }
                catch (Exception)
                {
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = data.DeserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(data.MessageIdentity, messageType, false, data.SendingPeer, data.Endpoint);

                    }
                }
            }
        }
    }
}