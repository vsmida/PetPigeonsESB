using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Dispatch
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IObjectFactory _objectFactory;
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly Dictionary<Type, MethodInfo> _messageTypeToCommandHandler = new Dictionary<Type, MethodInfo>();
        private readonly Dictionary<Type, List<MethodInfo>> _messageTypeToEventHandlers = new Dictionary<Type, List<MethodInfo>>();

        public MessageDispatcher(IObjectFactory objectFactory, IAssemblyScanner assemblyScanner)
        {
            _objectFactory = objectFactory;
            _assemblyScanner = assemblyScanner;
        }

        public void Dispatch(IMessage message)
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
            List<MethodInfo> eventHandlers;
            if (!_messageTypeToEventHandlers.TryGetValue(message.GetType(), out eventHandlers))
            {
                eventHandlers = _assemblyScanner.FindEventHandlersInAssemblies(message);
                _messageTypeToEventHandlers[message.GetType()] = eventHandlers;
            }

            foreach (var eventHandlerMethod in eventHandlers)
            {
                var instance = _objectFactory.GetInstance(eventHandlerMethod.DeclaringType);
                eventHandlerMethod.Invoke(instance, new[] { message });
            }
        }

        
        private void InvokeCommandHandler(IMessage message)
        {
            MethodInfo methodInfo;
            if (!_messageTypeToCommandHandler.TryGetValue(message.GetType(), out methodInfo))
            {
                var handlers = _assemblyScanner.FindCommandHandlersInAssemblies(message);

                if (!handlers.Any())
                    return;
                if (handlers.Count() > 1)
                    throw new Exception(string.Format("Multiple handlers present for command type {0} in app domain",
                                                      message.GetType().FullName));
                methodInfo = handlers.Single();
                _messageTypeToCommandHandler[message.GetType()] = methodInfo;
            }

            var instance = _objectFactory.GetInstance(methodInfo.DeclaringType);
            methodInfo.Invoke(instance, new[] { message });
        }

    }
}