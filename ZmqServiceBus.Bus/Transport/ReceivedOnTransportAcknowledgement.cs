using System;
using ProtoBuf;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport
{
    [ProtoContract]
    public class ReceivedOnTransportAcknowledgement : IMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;

        public ReceivedOnTransportAcknowledgement(Guid messageId)
        {
            MessageId = messageId;
        }
    }
}