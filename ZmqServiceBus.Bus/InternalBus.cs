using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DirectoryService.Commands;
using DirectoryService.Event;
using Shared;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : IBus
    {
        private readonly ITransport _transport;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IBusConfiguration _config;

        public InternalBus(ITransport transport, IMessageDispatcher dispatcher, IBusConfiguration config)
        {
            _transport = transport;
            _dispatcher = dispatcher;
            _config = config;
        }

        public void Send(ICommand command)
        {
            var transportMessage = GetTransportMessage(command);
            _transport.SendMessage(transportMessage);
        }

        private TransportMessage GetTransportMessage(IMessage command)
        {
            return new TransportMessage(Guid.NewGuid(), _config.ServiceIdentity, command.GetType().FullName, Serializer.Serialize(command));
        }

        public void Publish(IEvent message)
        {
            var transportMessage = GetTransportMessage(message);
            _transport.PublishMessage(transportMessage);
        }

        public void Initialize()
        {
            _transport.Initialize(_config.ServiceIdentity);
            _transport.OnMessageReceived += OnTransportMessageReceived;
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

            var registerCommand = new RegisterServiceRelevantMessages(_config.ServiceIdentity,
                                                                      _transport.Configuration.GetCommandsEnpoint(),
                                                                      _transport.Configuration.GetEventsEndpoint(),
                                                                      handledCommands.ToArray(), events.ToArray(), listenedEvents.ToArray());
            return registerCommand;
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
            _transport.RegisterCommandHandlerEndpoint<RegisterServiceRelevantMessages>(_config.DirectoryServiceCommandEndpoint);
            _transport.RegisterPublisherEndpoint<RegisteredHandlersForCommand>(_config.DirectoryServiceEventEndpoint);
            _transport.RegisterPublisherEndpoint<RegisteredPublishersForEvent>(_config.DirectoryServiceEventEndpoint);
        }

        private void OnTransportMessageReceived(ITransportMessage transportMessage)
        {
            var deserializedMessage = Serializer.Deserialize(transportMessage.Data, TypeUtils.Resolve(transportMessage.MessageType));
            try
            {
                _dispatcher.Dispatch(deserializedMessage as IMessage);
                _transport.AckMessage(transportMessage.SenderIdentity, transportMessage.MessageIdentity, true);
            }
            catch (Exception)
            {
                _transport.AckMessage(transportMessage.SenderIdentity, transportMessage.MessageIdentity, false);
            }

        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}