using System;
using System.Collections.Generic;
using System.Linq;
using DirectoryService.Commands;
using Shared;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : IBus
    {
        private readonly IReliabilityLayer _startupLayer;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IBusConfiguration _config;

        public InternalBus(IReliabilityLayer startupLayer, IMessageDispatcher dispatcher, IBusConfiguration config)
        {
            _startupLayer = startupLayer;
            _dispatcher = dispatcher;
            _config = config;
        }

        public void Send(ICommand command)
        {
            var transportMessage = GetTransportMessage(command);
            _startupLayer.Send(transportMessage);
        }

        private TransportMessage GetTransportMessage(IMessage command)
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
            RegisterWithDirectoryService();
        }

        private void RegisterWithDirectoryService()
        {
            var registerCommand = ScanAssembliesForRelevantTypes();
            Send(registerCommand);
        }

        private RegisterServiceRelevantMessages ScanAssembliesForRelevantTypes()
        {
            var events = new List<Type>();
            var handledCommands = new List<Type>();
            var listenedEvents = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsInterface && !type.IsAbstract)
                    {
                        if (IsEvent(type))
                            events.Add(type);

                        var commandHandlerInterfaces = type.GetInterfaces().Where(IsCommandHandler);
                        handledCommands.AddRange(commandHandlerInterfaces.Select(inter => inter.GetGenericArguments().First()));

                        var eventHandlerInterfaces = type.GetInterfaces().Where(IsEventHandler);
                        listenedEvents.AddRange(eventHandlerInterfaces.Select(inter => inter.GetGenericArguments().First()));
                    }
                }
            }


            //var registerCommand = new RegisterServiceRelevantMessages(_config.ServiceIdentity,
            //                                                          _transport.Configuration.GetCommandsEnpoint(),
            //                                                          _transport.Configuration.GetEventsEndpoint(),
            //                                                          handledCommands.ToArray(), events.ToArray(), listenedEvents.ToArray());
            //return registerCommand;
            return null;
        }

        private static bool IsEventHandler(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventHandler<>);
        }

        private static bool IsCommandHandler(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<>);
        }

        private static bool IsEvent(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IEvent));
        }

        private static bool IsCommand(Type type)
        {
            return type.GetInterfaces().Contains(typeof(ICommand));
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