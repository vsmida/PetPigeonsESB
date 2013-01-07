using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Dispatch
{
    public interface IAssemblyScanner
    {
        List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message);
        List<MethodInfo> FindEventHandlersInAssemblies(IMessage message);
        List<Type> GetHandledCommands();
        List<Type> GetHandledEvents();

        Dictionary<Type, ReliabilityLevel> FindMessagesInfosInAssemblies();
    }
}