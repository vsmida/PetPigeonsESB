using System;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ITransportMessage
    {
        string PeerName { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}