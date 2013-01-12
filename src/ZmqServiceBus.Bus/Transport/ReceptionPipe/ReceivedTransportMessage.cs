using System;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class ReceivedTransportMessage
    {
        public readonly string PeerName;
        public readonly string MessageType;
        public readonly Guid MessageIdentity;
        public readonly WireTransportType TransportType;
        public readonly byte[] Data;

        public ReceivedTransportMessage(string messageType, string peerName, Guid messageIdentity, WireTransportType transportType, byte[] data)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
            TransportType = transportType;
        }
    }
}