using System;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public struct ReceivedTransportMessage
    {
        public readonly string PeerName;
        public readonly string MessageType;
        public readonly Guid MessageIdentity;
        public readonly byte[] Data;

        public ReceivedTransportMessage(string messageType, string peerName, Guid messageIdentity, byte[] data)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
        }
    }
}