using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared;

namespace ZmqServiceBus.Bus
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IObjectFactory _objectFactory;
        private readonly Dictionary<Type, MethodInfo> _messageTypeToCommandHandler = new Dictionary<Type, MethodInfo>();
        private readonly Dictionary<Type, List<MethodInfo>> _messageTypeToEventHandlers = new Dictionary<Type, List<MethodInfo>>();

        public MessageDispatcher(IObjectFactory objectFactory)
        {
            _objectFactory = objectFactory;
        }

        public void Dispatch(IMessage message)
        {
            if (IsICommand(message))
            {
                InvokeCommandHandler(message);
            }

            if (IsIEvent(message))
            {
                InvokeEventHandlers(message);
            }

        }

        private void InvokeEventHandlers(IMessage message)
        {
            List<MethodInfo> eventHandlers;
            if (!_messageTypeToEventHandlers.TryGetValue(message.GetType(), out eventHandlers))
            {
                eventHandlers = FindEventHandlersInAssemblies(message);
                _messageTypeToEventHandlers[message.GetType()] = eventHandlers;
            }

            foreach (var eventHandlerMethod in eventHandlers)
            {
                var instance = _objectFactory.GetInstance(eventHandlerMethod.DeclaringType);
                eventHandlerMethod.Invoke(instance, new[] { message });
            }
        }

        private List<MethodInfo> FindEventHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                               (type.GetInterfaces().SingleOrDefault(
                                                   x => IsEventHandler(x, message.GetType())) != null)), "Handle");
        }

        private static bool IsIEvent(IMessage message)
        {
            return message.GetType().GetInterfaces().Contains(typeof(IEvent));
        }

        private void InvokeCommandHandler(IMessage message)
        {
            MethodInfo methodInfo;
            if (!_messageTypeToCommandHandler.TryGetValue(message.GetType(), out methodInfo))
            {
                var handlers = FindCommandHandlersInAssemblies(message);

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

        private List<MethodInfo> FindMethodsInAssemblyFromTypes(Predicate<Type> typeCondition, string methodName)
        {
            var methods = new List<MethodInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeCondition(type))
                        methods.Add(type.GetMethod(methodName));
                }
            }
            return methods;
        }

        private List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                                           (type.GetInterfaces().SingleOrDefault(
                                                               x => IsCommandHandler(x, message.GetType())) != null)), "Handle");
        }

        private static bool IsICommand(IMessage message)
        {
            return message.GetType().GetInterfaces().Contains(typeof(ICommand));
        }

        private static bool IsCommandHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<>) && type.GetGenericArguments().Single() == messageType;
        }

        private bool IsEventHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventHandler<>) && type.GetGenericArguments().Single() == messageType;
        }
    }
}