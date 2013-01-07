using System;
using System.Collections.Generic;
using ProtoBuf;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    [ProtoContract]
    public class MessageWireData
    {
        [ProtoMember(1, IsRequired = true)]
        public string MessageType { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public Guid MessageIdentity { get; private set; }
        [ProtoMember(3, IsRequired = true)]
        public byte[] Data { get; private set; }

        public MessageWireData(string messageType, Guid messageIdentity, byte[] data)
        {
            MessageType = messageType;
            MessageIdentity = messageIdentity;
            Data = data;
        }
    }

    public class WireSendingMessage
    {
       public MessageWireData MessageData { get; private set; }
       public IEndpoint Endpoint { get; private set; }

       public WireSendingMessage(MessageWireData messageData, IEndpoint endpoint)
       {
           this.MessageData = messageData;
           Endpoint = endpoint;
       }
    }
}