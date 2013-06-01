using System;
using System.Collections.Generic;
using System.Reflection;
using Bus.MessageInterfaces;
using Shared;

namespace Bus.Dispatch
{
    public interface IAssemblyScanner
    {
        List<MethodInfo> FindCommandHandlersInAssemblies(IMessage message);
        List<MethodInfo> FindEventHandlersInAssemblies(IMessage message);
        List<MessageOptions> GetMessageOptions(IEnumerable<Assembly> assembliesToScan = null);
        List<Type> GetHandledCommands();
        List<Type> GetHandledEvents();
    }
}