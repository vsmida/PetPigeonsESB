using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Dispatch
{
    public class AssemblyScanner : IAssemblyScanner
    {
        private List<MethodInfo> FindMethodsInAssemblyFromTypes(Predicate<Type> typeCondition, string methodName, Func<Type, Type[]> genericTypeArguments)
        {
            var methods = new List<MethodInfo>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeCondition(type))
                        methods.Add(type.GetMethod(methodName, genericTypeArguments(type)));
                }
            }
            return methods;
        }

        private static List<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().ToList();
        }

        public Dictionary<Type, ReliabilityLevel> FindMessagesInfosInAssemblies()
        {
            var result = new Dictionary<Type, ReliabilityLevel>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IMessage).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var reliability = type.GetCustomAttributes(typeof(BusReliability), false).SingleOrDefault() as BusReliability;
                        result.Add(type, reliability == null ? ReliabilityLevel.FireAndForget : reliability.ReliabilityLevel);
                    }
                }
            }
            return result;
        }

        public virtual List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                                           (type.GetInterfaces().SingleOrDefault(
                                                               x => IsCommandHandler(x, message.GetType())) != null)), "Handle", type => new[] { message.GetType() });
        }

        private static bool IsCommandHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<>) && type.GetGenericArguments().Single() == messageType;
        }

        private bool IsEventHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IBusEventHandler<>) && type.GetGenericArguments().Single() == messageType;
        }

        public List<MethodInfo> FindEventHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                                           (type.GetInterfaces().SingleOrDefault(
                                                               x => IsEventHandler(x, message.GetType())) != null)), "Handle", type => new[] { message.GetType() });
        }

        public virtual List<Type> GetHandledCommands()
        {
            var handledCommands = new HashSet<Type>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;
                    var commandHandlingInterfaces =
                        type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
                    foreach (var commandHandlingInterface in commandHandlingInterfaces)
                    {
                        handledCommands.Add(commandHandlingInterface.GetGenericArguments()[0]);
                    }
                }
            }
            return handledCommands.ToList();
        }

        public List<Type> GetHandledEvents()
        {
            var handledEvents = new HashSet<Type>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;
                    var commandHandlingInterfaces =
                        type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBusEventHandler<>));
                    foreach (var commandHandlingInterface in commandHandlingInterfaces)
                    {
                        handledEvents.Add(commandHandlingInterface.GetGenericArguments()[0]);
                    }
                }
            }
            return handledEvents.ToList();
        }
    }
}