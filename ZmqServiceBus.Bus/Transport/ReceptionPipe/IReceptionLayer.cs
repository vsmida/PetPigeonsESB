using System;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface IReceptionLayer : IDisposable
    {
        event Action<Transport.IReceivedTransportMessage> OnMessageReceived;
        void Initialize();
    }
}