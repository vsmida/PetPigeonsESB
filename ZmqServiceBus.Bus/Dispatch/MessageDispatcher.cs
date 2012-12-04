using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Dispatch
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private class HandlerDispatcher
        {
            public Type MessageType;
            public MethodInfo MethodToInvoke;
            public bool IsInfrastructure;

            public HandlerDispatcher(Type messageType, MethodInfo methodToInvoke)
            {
                MessageType = messageType;
                MethodToInvoke = methodToInvoke;
            }
        }


        private readonly IObjectFactory _objectFactory;
        private volatile bool _running = true;
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly Dictionary<Type, HandlerDispatcher> _messageTypeToCommandHandler = new Dictionary<Type, HandlerDispatcher>();
        private readonly Dictionary<Type, List<HandlerDispatcher>> _messageTypeToEventHandlers = new Dictionary<Type, List<HandlerDispatcher>>();
        private readonly BlockingCollection<IMessage> _standardMessagesToDispatch = new BlockingCollection<IMessage>();
        private readonly Dictionary<Type, bool> _messageTypeToInfrastructureCondition = new Dictionary<Type, bool>();
        public event Action<IMessage, Exception> ErrorOccurred = delegate { };
        public event Action<IMessage> SuccessfulDispatch = delegate {};


        public MessageDispatcher(IObjectFactory objectFactory, IAssemblyScanner assemblyScanner)
        {
            _objectFactory = objectFactory;
            _assemblyScanner = assemblyScanner;

            new Thread(() =>
                                     {
                                         while (_running)
                                         {
                                             IMessage message;
                                             if (_standardMessagesToDispatch.TryTake(out message,
                                                                                     TimeSpan.FromMilliseconds(500)))
                                             {
                                                 InvokeHandlers(message);
                                             }
                                         }


                                     }).Start();
        }

        public void Dispatch(IMessage message)
        {
            bool isInfrastructureMessage;
            if (!_messageTypeToInfrastructureCondition.TryGetValue(message.GetType(), out isInfrastructureMessage))
            {
                isInfrastructureMessage = IsInfrastructure(message);
                _messageTypeToInfrastructureCondition[message.GetType()] = isInfrastructureMessage;
            }
            if (isInfrastructureMessage)
                InvokeHandlers(message);
            else
                _standardMessagesToDispatch.TryAdd(message);
        }

        private void InvokeHandlers(IMessage message)
        {

            try
            {
                if (message.IsICommand())
                {
                    InvokeCommandHandler(message);
                }

                if (message.IsIEvent())
                {
                    InvokeEventHandlers(message);
                }

                SuccessfulDispatch(message);
            }
            catch (Exception e)
            {
                ErrorOccurred(message, e);
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

        private static bool IsInfrastructure(IMessage message)
        {
            return message.GetType().GetCustomAttributes(typeof(InfrastructureMessageAttribute),
                                                         true).Any();
        }

        public void Dispose()
        {
            _running = false;
        }
    }
}