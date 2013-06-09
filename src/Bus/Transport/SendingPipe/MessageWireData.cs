using System;
using ProtoBuf;

namespace Bus.Transport.SendingPipe
{
    [ProtoContract]
    public class MessageWireData
    {
        [ProtoMember(1, IsRequired = true)]
        public string MessageType { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public Guid MessageIdentity { get; private set; }
        [ProtoMember(3, IsRequired = true)]
        public PeerId SendingPeerId { get; private set; }
        [ProtoMember(4, IsRequired = true)]
        public byte[] Data { get; private set; }
        [ProtoMember(5, IsRequired = true)]
        public int? SequenceNumber { get; set; }

        public MessageWireData(string messageType, Guid messageIdentity, PeerId sendingPeer, byte[] data)
        {
            MessageType = messageType;
            MessageIdentity = messageIdentity;
            Data = data;
            SendingPeerId = sendingPeer;
        }

        private MessageWireData()
        {
        }
    }
}