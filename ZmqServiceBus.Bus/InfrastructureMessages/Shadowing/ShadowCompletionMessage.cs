using System;
using ProtoBuf;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class ShadowCompletionMessage : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;
        [ProtoMember(2, IsRequired = true)]
        public readonly string FromPeer;
        [ProtoMember(3, IsRequired = true)]
        public readonly string ToPeer;
        [ProtoMember(4, IsRequired = true)]
        public readonly bool ProcessingSuccessful;
        [ProtoMember(5, IsRequired = true)]
        public readonly WireTransportType TransportType;
        [ProtoMember(6, IsRequired = true)]
        public readonly string MessageType;

        public ShadowCompletionMessage(Guid messageId, string fromPeer, string toPeer, bool processingSuccessful, WireTransportType transportType, string messageType)
        {
            MessageId = messageId;
            FromPeer = fromPeer;
            ToPeer = toPeer;
            ProcessingSuccessful = processingSuccessful;
            TransportType = transportType;
            MessageType = messageType;
        }
    }
}