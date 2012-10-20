using System;

namespace ZmqServiceBus.Transport
{
    public interface ITransportMessage
    {
        string SenderIdentity { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }

    public class TransportMessage : ITransportMessage
    {
        public string SenderIdentity { get; private set; }
        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }

        public TransportMessage(Guid messageIdentity, string senderIdentity, string messageType, byte[] data)
        {
            MessageIdentity = messageIdentity;
            SenderIdentity = senderIdentity;
            MessageType = messageType;
            Data = data;
        }
    }
}