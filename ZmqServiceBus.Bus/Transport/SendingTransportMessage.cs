using System;

namespace ZmqServiceBus.Bus.Transport
{
    public class SendingTransportMessage : ISendingTransportMessage
    {
        public SendingTransportMessage(string messageType, Guid messageIdentity, byte[] data)
        {
            Data = data;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
        }

        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }
    }
}