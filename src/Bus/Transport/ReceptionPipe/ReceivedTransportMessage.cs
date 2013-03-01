using System;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class ReceivedTransportMessage
    {
        public string PeerName;
        public string MessageType;
        public Guid MessageIdentity;
        public IEndpoint Endpoint;
        public byte[] Data;
        public int? SequenceNumber;

        public ReceivedTransportMessage(string messageType, string peerName, Guid messageIdentity, IEndpoint endpoint, byte[] data, int? sequenceNumber)
        {
            PeerName = peerName;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
            SequenceNumber = sequenceNumber;
            Endpoint = endpoint;
        }

        public ReceivedTransportMessage()
        {
            
        }

        public void Reinitialize(string messageType, string peerName, Guid messageIdentity, ZmqEndpoint endpoint, byte[] data, int? sequenceNumber)
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