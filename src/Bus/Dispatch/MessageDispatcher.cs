using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bus.MessageInterfaces;
using StructureMap;

namespace Bus.Dispatch
{
    class MessageDispatcher : IMessageDispatcher
    {
        private class HandlerDispatcher
        {
            public Type MessageType;
            public MethodInfo MethodToInvoke;

            public HandlerDispatcher(Type messageType, MethodInfo methodToInvoke)
            {
                MessageType = messageType;
                MethodToInvoke = methodToInvoke;
            }
        }

        private readonly IContainer _objectFactory;
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly Dictionary<Type, HandlerDispatcher> _messageTypeToCommandHandler = new Dictionary<Type, HandlerDispatcher>();
        private readonly Dictionary<Type, List<HandlerDispatcher>> _messageTypeToEventHandlers = new Dictionary<Type, List<HandlerDispatcher>>();


        public MessageDispatcher(IContainer objectFactory, IAssemblyScanner assemblyScanner)
        {
            _objectFactory = objectFactory;
            _assemblyScanner = assemblyScanner;
        }

        public void Dispatch(IMessage message)
        {
            InvokeHandlers(message);
        }

        private void InvokeHandlers(IMessage message)
        {
            if (message.IsICommand())
            {
                InvokeCommandHandler(message);
            }

            if (message.IsIEvent())
            {
                InvokeEventHandlers(message);
            }
        }


        private void InvokeEventHandlers(IMessage message)
        {
            List<HandlerDispatcher> eventHandlers;
            if (!_messageTypeToEventHandlers.TryGetValue(message.GetType(), out eventHandlers))
            {
                var methods = _assemblyScanner.FindEventHandlersInAssemblies(message) ?? Enumerable.Empty<MethodInfo>();
                eventHandlers = methods.Select(x => new HandlerDispatcher(message.GetType(), x)).ToList();
                _messageTypeToEventHandlers[message.GetType()] = eventHandlers;
            }

            foreach (var eventDispatcher in eventHandlers)
            {
                var instance = _objectFactory.GetInstance(eventDispatcher.MethodToInvoke.DeclaringType);
                eventDispatcher.MethodToInvoke.Invoke(instance, new object[] { message });
            }
        }


        private void InvokeCommandHandler(IMessage message)
        {
            HandlerDispatcher handlerDispatcher;
            if (!_messageTypeToCommandHandler.TryGetValue(message.GetType(), out handlerDispatcher))
            {
                var handlers = _assemblyScanner.FindCommandHandlersInAssemblies(message) ?? Enumerable.Empty<MethodInfo>();

                if (!handlers.Any())
                    return;
                if (handlers.Count() > 1)
                    throw new Exception(string.Format("Multiple handlers present for command type {0} in app domain",
                                                      message.GetType().FullName));
                var methodInfo = handlers.Single();

                handlerDispatcher = new HandlerDispatcher(message.GetType(), methodInfo);
                _messageTypeToCommandHandler[message.GetType()] = handlerDispatcher;
            }

            var instance = _objectFactory.GetInstance(handlerDispatcher.MethodToInvoke.DeclaringType);
            handlerDispatcher.MethodToInvoke.Invoke(instance, new object[] { message });
        }


    }
}