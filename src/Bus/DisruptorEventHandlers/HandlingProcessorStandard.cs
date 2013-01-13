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
    public class HandlingProcessorStandard : IEventHandler<InboundBusinessMessageEntry>
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;

        public HandlingProcessorStandard(IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void OnNext(InboundBusinessMessageEntry data, long sequence, bool endOfBatch)
        {
            HandleMessage(data.DeserializedMessage, data.SendingPeer, data.TransportType, data.MessageIdentity);
        }

        private void HandleMessage(IMessage deserializedMessage, string sendingPeer, WireTransportType transportType, Guid messageId)
        {
            using (MessageContext.SetContext(sendingPeer, transportType))
            {
                try
                {
                    _dispatcher.Dispatch(deserializedMessage);
                    if (!(deserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = deserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(messageId, messageType, true,sendingPeer, transportType);
                    }
                }
                catch (Exception)
                {
                    if (!(deserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = deserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(messageId, messageType, false, sendingPeer, transportType);
                    }
                }
            }
        }
    }


    public class HandlingProcessorInfrastructure : IEventHandler<InboundInfrastructureEntry>
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;

        public HandlingProcessorInfrastructure(IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void OnNext(InboundInfrastructureEntry data, long sequence, bool endOfBatch)
        {
            using (var context = MessageContext.SetContext(data.SendingPeer, data.TransportType))
            {
                try
                {
                    _dispatcher.Dispatch(data.DeserializedMessage);
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = data.DeserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(data.MessageIdentity, messageType, true, data.SendingPeer, data.TransportType);
                    }
                }
                catch (Exception)
                {
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                    {
                        var messageType = data.DeserializedMessage.GetType().FullName;
                        _messageSender.Acknowledge(data.MessageIdentity, messageType, false, data.SendingPeer, data.TransportType);

                    }
                }
            }
        }
    }
}