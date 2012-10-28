using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared;

namespace ZmqServiceBus.Bus
{
    public interface IAssemblyScanner
    {
        List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message);
        List<MethodInfo> FindEventHandlersInAssemblies(IMessage message);
        List<Type> GetHandledCommands();
        List<Type> GetHandledEvents();
    }

    public class AssemblyScanner : IAssemblyScanner
    {
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
            throw new NotImplementedException();
        }

        public List<Type> GetHandledEvents()
        {
            throw new NotImplementedException();
        }
    }
}