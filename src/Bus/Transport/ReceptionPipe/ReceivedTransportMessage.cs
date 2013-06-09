using System;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.Transport.ReceptionPipe
{
    [ProtoContract]
    public class ReceivedTransportMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public PeerId PeerId;
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

        public ReceivedTransportMessage(string messageType, PeerId peerId, Guid messageIdentity, IEndpoint endpoint, byte[] data, int? sequenceNumber)
        {
            PeerId = peerId;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
            SequenceNumber = sequenceNumber;
            Endpoint = endpoint;
        }

        public ReceivedTransportMessage()
        {
            
        }

        public void Reinitialize(string messageType, PeerId peerId, Guid messageIdentity, IEndpoint endpoint, byte[] data, int? sequenceNumber)
        {
            PeerId = peerId;
            MessageIdentity = messageIdentity;
            MessageType = messageType;
            Data = data;
            SequenceNumber = sequenceNumber;
            Endpoint = endpoint;
        }
    }
}