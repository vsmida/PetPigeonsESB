using System;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface ISendingTransportMessage
    {
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}