using System;
using Shared;

namespace ZmqServiceBus.Transport
{
    public interface IReliabilityLayer : IDisposable
    {
        void RegisterMessageReliabilitySetting(Type messageType, MessageOptions level);
        void Send(ITransportMessage message);
        void Publish(ITransportMessage message);
        void Route(ITransportMessage message);
        event Action<ITransportMessage> OnMessageReceived;
        void Initialize();
    }
}