using System;
using System.Collections.Generic;
using System.Linq;
using DirectoryService.Commands;
using Shared;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Bus
{
    public interface IBusBootstrapperConfiguration
    {
        string DirectoryServiceCommandEndpoint { get; }
        string DirectoryServiceEventEndpoint { get; }
    }

    public interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

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

        private ITransportMessage GetTransportMessage(IMessage command)
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
            RegisterDirectoryServiceEndpoints();
        }

        public void Reply(IMessage message)
        {
        }


        private void RegisterDirectoryServiceEndpoints()
        {
            //_transport.RegisterCommandHandlerEndpoint<RegisterServiceRelevantMessages>(_config.DirectoryServiceCommandEndpoint);
            //_transport.RegisterPublisherEndpoint<RegisteredHandlersForCommand>(_config.DirectoryServiceEventEndpoint);
            //_transport.RegisterPublisherEndpoint<RegisteredPublishersForEvent>(_config.DirectoryServiceEventEndpoint);
        }

        private void OnTransportMessageReceived(ITransportMessage transportMessage)
        {
            var deserializedMessage = Serializer.Deserialize(transportMessage.Data, TypeUtils.Resolve(transportMessage.MessageType));
            try
            {
                _dispatcher.Dispatch(deserializedMessage as IMessage);
            //    _transport.AckMessage(transportMessage.SendingSocketId, transportMessage.MessageIdentity, true);
            }
            catch (Exception)
            {
       //         _transport.AckMessage(transportMessage.SendingSocketId, transportMessage.MessageIdentity, false);
            }

        }

        public void Dispose()
        {
            _startupLayer.Dispose();
        }
    }
}