using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class SendingBusMessage : ISendingBusMessage
    {
        public SendingBusMessage(string messageType, Guid messageIdentity, byte[] data, IEnumerable<IEndpoint> targetEndpoints)
        {
            Data = data;
            TargetEndpoints = targetEndpoints;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
        }

        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }
        public IEnumerable<IEndpoint> TargetEndpoints { get; private set; }
    }
}