using System;

namespace ZmqServiceBus.Bus.Transport
{
    public class ReceivedTransportMessage : IReceivedTransportMessage
    {
        public string PeerName { get; private set; }
        public string MessageType { get; private set; }
        public Guid MessageIdentity { get; private set; }
        public byte[] Data { get; private set; }

        public ReceivedTransportMessage(string messageType, string peerName, Guid messageIdentity, byte[] data)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
        }
    }
}