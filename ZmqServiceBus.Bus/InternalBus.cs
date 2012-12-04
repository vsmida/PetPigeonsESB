using System;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;
using IReceivedTransportMessage = ZmqServiceBus.Bus.Transport.IReceivedTransportMessage;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : MarshalByRefObject, IBus, IReplier
    {
        private readonly IReceptionLayer _startupLayer;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;
        private readonly IBusBootstrapper _busBootstrapper;

        public InternalBus(IReceptionLayer startupLayer, IMessageDispatcher dispatcher, IMessageSender messageSender, IBusBootstrapper busBootstrapper)
        {
            _startupLayer = startupLayer;
            _dispatcher = dispatcher;
            _messageSender = messageSender;
            _busBootstrapper = busBootstrapper;
        }

        public IBlockableUntilCompletion Send(ICommand command)
        {
           return _messageSender.Send(command);
        }

        public void Publish(IEvent message)
        {
            _messageSender.Publish(message);
        }

        public void Initialize()
        {
            _startupLayer.Initialize();
            _startupLayer.OnMessageReceived += OnTransportMessageReceived;
            _busBootstrapper.BootStrapTopology();
        }

        public void Reply(IMessage message)
        {
            _messageSender.Route(message, MessageContext.PeerName);
        }

        private void OnTransportMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            bool successfulDispatch = true;
            try
            {
                //if acknowledgement 
                //calbackdispatcherManager.executeCallback(message)
                //ok AddCallbaclk
                var deserializedMessage = Serializer.Deserialize(receivedTransportMessage.Data, TypeUtils.Resolve(receivedTransportMessage.MessageType));
                _dispatcher.Dispatch(deserializedMessage as IMessage);
            }
            catch (Exception)
            {
                successfulDispatch = false;
            }
            if(receivedTransportMessage.MessageType != typeof(CompletionAcknowledgementMessage).FullName)
            _messageSender.Route(new CompletionAcknowledgementMessage(receivedTransportMessage.MessageIdentity, successfulDispatch), receivedTransportMessage.PeerName);


        }

        public void Dispose()
        {
            _startupLayer.Dispose();
            _dispatcher.Dispose();
        }
    }
}