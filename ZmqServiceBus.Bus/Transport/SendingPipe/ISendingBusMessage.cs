using System;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface ISendingBusMessage
    {
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }
}