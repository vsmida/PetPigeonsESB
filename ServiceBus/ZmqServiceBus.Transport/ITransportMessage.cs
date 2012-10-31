using System;

namespace ZmqServiceBus.Transport
{
    public interface ITransportMessage
    {
        byte[] SendingSocketId { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }

    public class TransportMessage : ITransportMessage
    {
        public byte[] SendingSocketId { get; private set; }
        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }

        public TransportMessage(Guid messageIdentity, byte[] sendingSocketId, string messageType, byte[] data)
        {
            MessageIdentity = messageIdentity;
            SendingSocketId = sendingSocketId;
            MessageType = messageType;
            Data = data;
        }
    }
}