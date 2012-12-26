using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface ISendingBusMessage
    {
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
        IEnumerable<IEndpoint> TargetEndpoints { get; }
    }
}