using System.Collections.Generic;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface IStartupReliabilityStrategy
    {
        string PeerName { get; }
        string MessageType { get; }
        bool IsInitialized { get; }
        IEnumerable<IReceivedTransportMessage> GetMessagesToBubbleUp(IReceivedTransportMessage message); //enqueue or release messages when broker is sending same message as client.
    }
}