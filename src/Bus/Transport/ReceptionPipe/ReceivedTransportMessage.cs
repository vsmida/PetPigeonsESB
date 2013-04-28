using System;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.Transport.ReceptionPipe
{
    [ProtoContract]
    public class ReceivedTransportMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName;
        [ProtoMember(2, IsRequired = true)]
        public string MessageType;
        [ProtoMember(3, IsRequired = true)]
        public Guid MessageIdentity;
        [ProtoMember(4, IsRequired = true)]
        public IEndpoint Endpoint;
        [ProtoMember(5, IsRequired = true)]
        public byte[] Data;
        [ProtoMember(6, IsRequired = true)]
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