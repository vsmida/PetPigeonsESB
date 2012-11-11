using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Core
{
    public class ServiceStore : IServiceStore
    {
        private readonly Dictionary<Type, HashSet<string>> typeToEndpoints = new Dictionary<Type, HashSet<string>>();

        public IEnumerable<string> GetCommandEndpoints<T>() where T : ICommand
        {
            return typeToEndpoints[typeof (T)] ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetEventEndpoints<T>() where T : IEvent
        {
            return typeToEndpoints[typeof(T)] ?? Enumerable.Empty<string>();
        }

        public void RegisterCommandHandler<T>(string endpoint) where T : ICommand
        {
            typeToEndpoints.GetOrCreateNew(typeof(T)).Add(endpoint);
        }

        public void RegisterEventPublisher<T>(string endpoint) where T : IEvent
        {
            typeToEndpoints.GetOrCreateNew(typeof(T)).Add(endpoint);
        }
    }
}