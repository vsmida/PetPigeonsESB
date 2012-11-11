using System;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ISendingTransportMessage
    {
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}