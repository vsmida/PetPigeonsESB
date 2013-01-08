using System;
using Disruptor;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.DisruptorEventHandlers
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
            using (var context = MessageContext.SetContext(data.SendingPeer, data.TransportType))
            {
                try
                {

                    _dispatcher.Dispatch(data.DeserializedMessage);
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                        _messageSender.Acknowledge(data.MessageIdentity, true, data.SendingPeer,data.TransportType);


                }
                catch (Exception)
                {
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                        _messageSender.Acknowledge(data.MessageIdentity, false, data.SendingPeer, data.TransportType);
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
                        _messageSender.Acknowledge(data.MessageIdentity, true, data.SendingPeer, data.TransportType);
                }
                catch (Exception)
                {
                    if (!(data.DeserializedMessage is CompletionAcknowledgementMessage))
                        _messageSender.Acknowledge(data.MessageIdentity, false, data.SendingPeer, data.TransportType);
                }
            }
        }
    }
}