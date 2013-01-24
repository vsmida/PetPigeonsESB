using System;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class ReceivedTransportMessage
    {
        public readonly string PeerName;
        public readonly string MessageType;
        public readonly Guid MessageIdentity;
        public readonly IEndpoint Endpoint;
        public readonly byte[] Data;
        public readonly int? SequenceNumber;

        public ReceivedTransportMessage(string messageType, string peerName, Guid messageIdentity, IEndpoint endpoint, byte[] data, int? sequenceNumber)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
            SequenceNumber = sequenceNumber;
            Endpoint = endpoint;
        }
    }
}