using System;

namespace ZmqServiceBus.Contracts
{
    public interface ITransportMessage
    {
        string PeerName { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}