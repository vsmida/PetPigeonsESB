using System;
using Shared;

namespace ZmqServiceBus.Transport
{
    public interface IBus : IDisposable
    {
        void Initialize(ITransportConfiguration config);
        void RegisterEventPublisher<T>(string endpoint) where T:IEvent;
        void RegisterCommandHandler<T>(string endpoint) where T : ICommand;
        void SendCommand<T>(T command) where T : ICommand;
        void PublishEvent<T>(T message) where T : IEvent;
        

    }
}