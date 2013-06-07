using System;
using System.Collections.Generic;
using System.Reflection;
using Bus.MessageInterfaces;
using Bus.Serializer;
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
        List<Type> GetSubscriptionFilterTypes(IEnumerable<Assembly> assemblies = null);
        Dictionary<Type, Type> FindMessageSerializers(IEnumerable<Assembly> assemblies = null);
        Dictionary<Type, Type> FindEndpointTypesToSerializers(IEnumerable<Assembly> assemblies = null);
        List<Type> FindIEndpointTypes(IEnumerable<Assembly> assemblies = null);
    }
}