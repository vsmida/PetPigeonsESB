using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bus.Attributes;
using Bus.MessageInterfaces;
using Bus.Subscriptions;
using Bus.Transport.Network;
using Shared;

namespace Bus.Dispatch
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


        public List<MessageOptions> GetMessageOptions(IEnumerable<Assembly> assembliesToScan = null)
        {
            var options = new List<MessageOptions>();
            var assemblies = assembliesToScan ?? GetAssemblies();

            var typeToFilter = GetSubscriptionFilters(assemblies);

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract || (!typeof(IMessage).IsAssignableFrom(type)))
                        continue;
                    var reliability = type.GetCustomAttributes(typeof(BusOptionsAttribute), true).SingleOrDefault() as BusOptionsAttribute;
                    if (options.All(x => x.MessageType != type))
                        options.Add(new MessageOptions(type, reliability == null ? ReliabilityLevel.FireAndForget : reliability.ReliabilityLevel,
                                                                    reliability == null ? WireTransportType.ZmqPushPullTransport : reliability.TransportType,
                                                                    typeToFilter.GetValueOrDefault(type)));
                }
            }

            return options;
        }

        private static Dictionary<Type, ISubscriptionFilter> GetSubscriptionFilters(IEnumerable<Assembly> assemblies)
        {
            var typeToFilter = new Dictionary<Type, ISubscriptionFilter>();
            foreach (var assembly in assemblies)
            {
                foreach (
                    var type in
                        assembly.GetTypes().Where(
                            type =>
                            typeof (ISubscriptionFilter).IsAssignableFrom(type)  && !type.IsAbstract))
                {
                    var filterAttribute =
                        type.GetCustomAttributes(typeof (SubscriptionFilterAttributeActive), true).SingleOrDefault() as
                        SubscriptionFilterAttributeActive;
                    if (filterAttribute == null || filterAttribute.Active)
                    {
                        var typeGenericParameter = type.GetInterfaces().Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISubscriptionFilter<>)).GetGenericArguments()[0];
                        typeToFilter[typeGenericParameter] = Activator.CreateInstance(type, true) as ISubscriptionFilter;
                    }
                }
            }
            return typeToFilter;
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