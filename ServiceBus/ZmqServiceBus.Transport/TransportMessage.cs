using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Transport
{
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