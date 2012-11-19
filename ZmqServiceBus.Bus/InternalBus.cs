using System;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : IBus, IReplier
    {
        private readonly IReceptionLayer _startupLayer;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;

        public InternalBus(IReceptionLayer startupLayer, IMessageDispatcher dispatcher, IMessageSender messageSender)
        {
            _startupLayer = startupLayer;
            _dispatcher = dispatcher;
            _messageSender = messageSender;
        }

        public void Send(ICommand command)
        {
           _messageSender.Send(command);
        }

        public void Publish(IEvent message)
        {
            _messageSender.Publish(message);
        }

        public void Initialize()
        {
            _startupLayer.Initialize();
            _startupLayer.OnMessageReceived += OnTransportMessageReceived;
        }

        public void Reply(IMessage message)
        {
            _messageSender.Route(message, MessageContext.PeerName);
        }

        private void OnTransportMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            var deserializedMessage = Serializer.Deserialize(receivedTransportMessage.Data, TypeUtils.Resolve(receivedTransportMessage.MessageType));
            try
            {
                _dispatcher.Dispatch(deserializedMessage as IMessage);
            }
            catch (Exception)
            {
            }

        }

        public void Dispose()
        {
            _startupLayer.Dispose();
        }
    }
}