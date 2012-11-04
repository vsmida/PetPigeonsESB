using System;

namespace ZmqServiceBus.Transport
{
    public interface ITransportMessage
    {
        string PeerName { get; }
        string MessageType { get; }
        Guid MessageIdentity { get; }
        byte[] Data { get; }
    }

    public class TransportMessage : ITransportMessage
    {
        public string PeerName { get; private set; }
        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }

        public TransportMessage(string messageType, string peerName, Guid messageIdentity, byte[] data)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
        }
    }
}