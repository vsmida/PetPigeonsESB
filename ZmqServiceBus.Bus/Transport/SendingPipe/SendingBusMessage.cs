using System;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class SendingBusMessage : ISendingBusMessage
    {
        public SendingBusMessage(string messageType, Guid messageIdentity, byte[] data)
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