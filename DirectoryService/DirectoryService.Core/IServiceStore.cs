using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Core
{
    public interface IServiceStore
    {
        IEnumerable<string> GetCommandEndpoints<T>() where T: ICommand;
        IEnumerable<string> GetEventEndpoints<T>() where T: IEvent;
        void RegisterCommandHandler<T>(string endpoint) where T : ICommand;
        void RegisterEventPublisher<T>(string endpoint) where T : IEvent;
    }
}