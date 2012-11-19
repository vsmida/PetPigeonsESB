using System;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface IReceptionLayer : IDisposable
    {
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void Initialize();
    }
}