using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bus.MessageInterfaces;
using Disruptor;
using StructureMap;

namespace Bus.Dispatch
{
    class MessageDispatcher : IMessageDispatcher
    {
        private class HandlerDispatcher
        {
            public readonly Type MessageType;
            public readonly Action<object, IMessage> HandleMethod;
            public readonly Type HandlerType;
            public readonly object Instance;

            public HandlerDispatcher(Type messageType, Action<object, IMessage> handleMethod, Type handlerType, object instance)
            {
                MessageType = messageType;
                HandleMethod = handleMethod;
                HandlerType = handlerType;
                Instance = instance;
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

        private Action<object, IMessage> GenerateHandleAction(Type interfaceType)
        {
            var methodInfo = interfaceType.GetMethod("Handle");
            var messageType = interfaceType.GetGenericArguments()[0];

            var instance = Expression.Parameter(typeof(object), "instance");
            var message = Expression.Parameter(typeof(IMessage), "message");
            var body = Expression.Call(Expression.Convert(instance, interfaceType), methodInfo, Expression.Convert(message, messageType));
            var lambda = Expression.Lambda(typeof(Action<object, IMessage>), body, instance, message);
            return (Action<object, IMessage>)lambda.Compile();
        }


        private void InvokeHandlers(IMessage message)
        {
            if (message is ICommand)
            {
                InvokeCommandHandler(message);
            }

            if (message is IEvent)
            {
                InvokeEventHandlers(message);
            }
        }


        private void InvokeEventHandlers(IMessage message)
        {
            List<HandlerDispatcher> eventHandlers;
            if (!_messageTypeToEventHandlers.TryGetValue(message.GetType(), out eventHandlers))
            {
                var handlerInfos = _assemblyScanner.FindEventHandlersInAssemblies(message) ?? Enumerable.Empty<HandlerInfo>();
                var handlertype = typeof(IBusEventHandler<>);
                eventHandlers = handlerInfos.Select(x => new HandlerDispatcher(message.GetType(), GenerateHandleAction(handlertype.MakeGenericType(message.GetType())),
                                                                               x.HandleMethod.DeclaringType, x.IsStatic ? _objectFactory.GetInstance(x.HandleMethod.DeclaringType) : null)).ToList();
                _messageTypeToEventHandlers[message.GetType()] = eventHandlers;
            }

            foreach (var eventDispatcher in eventHandlers)
            {
                var instance = eventDispatcher.Instance ?? _objectFactory.GetInstance(eventDispatcher.HandlerType);
                eventDispatcher.HandleMethod(instance, message);
            }
        }


        private void InvokeCommandHandler(IMessage message)
        {
            HandlerDispatcher handlerDispatcher;
            if (!_messageTypeToCommandHandler.TryGetValue(message.GetType(), out handlerDispatcher))
            {
                var handlers = _assemblyScanner.FindCommandHandlersInAssemblies(message) ?? Enumerable.Empty<HandlerInfo>();

                if (!handlers.Any())
                    return;
                if (handlers.Count() > 1)
                    throw new Exception(string.Format("Multiple handlers present for command type {0} in app domain",
                                                      message.GetType().FullName));
                var methodInfo = handlers.Single();
                var handlertype = typeof(ICommandHandler<>);
                handlerDispatcher = new HandlerDispatcher(message.GetType(), GenerateHandleAction(handlertype.MakeGenericType(message.GetType())), methodInfo.HandleMethod.DeclaringType,
                                                                                                  methodInfo.IsStatic ? _objectFactory.GetInstance(methodInfo.HandleMethod.DeclaringType) : null);
                _messageTypeToCommandHandler[message.GetType()] = handlerDispatcher;
            }
            var instance = handlerDispatcher.Instance ?? _objectFactory.GetInstance(handlerDispatcher.HandlerType);
            handlerDispatcher.HandleMethod(instance, message);
        }


    }
}