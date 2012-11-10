using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface IAssemblyScanner
    {
        List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message);
        List<MethodInfo> FindEventHandlersInAssemblies(IMessage message);
        List<Type> GetHandledCommands();
        List<Type> GetHandledEvents();
        List<Type> GetSentEvents();

    }

    public class AssemblyScanner : IAssemblyScanner
    {
        private List<MethodInfo> FindMethodsInAssemblyFromTypes(Predicate<Type> typeCondition, string methodName)
        {
            var methods = new List<MethodInfo>();
            var assemblies = GetAssemblies();
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

        private static List<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().ToList();
        }

        public List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                                           (type.GetInterfaces().SingleOrDefault(
                                                               x => IsCommandHandler(x, message.GetType())) != null)), "Handle");
        }
        
        private static bool IsCommandHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<>) && type.GetGenericArguments().Single() == messageType;
        }

        private bool IsEventHandler(Type type, Type messageType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventHandler<>) && type.GetGenericArguments().Single() == messageType;
        }

        public List<MethodInfo> FindEventHandlersInAssemblies(IMessage message)
        {
            return FindMethodsInAssemblyFromTypes(type => ((!type.IsInterface && !type.IsAbstract) &&
                                               (type.GetInterfaces().SingleOrDefault(
                                                   x => IsEventHandler(x, message.GetType())) != null)), "Handle");
        }

        public List<Type> GetHandledCommands()
        {
            HashSet<Type> handledCommands = new HashSet<Type>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if(type.IsInterface || type.IsAbstract)
                        continue;
                    var commandHandlingInterfaces =
                        type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (ICommandHandler<>));
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
            HashSet<Type> handledEvents = new HashSet<Type>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;
                    var commandHandlingInterfaces =
                        type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandler<>));
                    foreach (var commandHandlingInterface in commandHandlingInterfaces)
                    {
                        handledEvents.Add(commandHandlingInterface.GetGenericArguments()[0]);
                    }
                }
            }
            return handledEvents.ToList();
        }

        public List<Type> GetSentEvents()
        {
            HashSet<Type> handledEvents = new HashSet<Type>();
            var assemblies = GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if(typeof(IEvent).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    handledEvents.Add(type);       }
            }
            return handledEvents.ToList();
        }
    }
}