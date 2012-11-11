using System;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IReceivedTransportMessage
    {
        string PeerName { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}