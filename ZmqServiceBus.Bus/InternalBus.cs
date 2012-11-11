using System;
using Shared;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : IBus, IReplier
    {
        private readonly IReliabilityLayer _startupLayer;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IBusBootstrapper _bootstrapper;

        public InternalBus(IReliabilityLayer startupLayer, IMessageDispatcher dispatcher, IBusBootstrapper bootstrapper)
        {
            _startupLayer = startupLayer;
            _dispatcher = dispatcher;
            _bootstrapper = bootstrapper;
        }

        public void Send(ICommand command)
        {
            var transportMessage = GetTransportMessage(command);
            _startupLayer.Send(transportMessage);
        }

        private ISendingTransportMessage GetTransportMessage(IMessage command)
        {
          //  return new TransportMessage(Guid.NewGuid(), command.GetType().FullName, Serializer.Serialize(command));
            return null;
        }

        public void Publish(IEvent message)
        {
            var transportMessage = GetTransportMessage(message);
            _startupLayer.Publish(transportMessage);
        }

        public void Initialize()
        {
            _startupLayer.Initialize();
            _startupLayer.OnMessageReceived += OnTransportMessageReceived;
            _bootstrapper.BootStrapTopology();
        }

        public void Reply(IMessage message)
        {
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