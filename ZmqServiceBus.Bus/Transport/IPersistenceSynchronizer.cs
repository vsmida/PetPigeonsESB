using System;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IPersistenceSynchronizer
    {
        event Action<string> MessageTypeSynchronizationRequested;
        event Action<string, string> MessageTypeForPeerSynchronizationRequested;
        void SynchronizeMessageType(string messageType);
        Void SynchronizeMessageType(string messageType, string peer);
    }
}