using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Shadowing
{
    [ProtoContract]
    class ShadowCompletionMessage : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Guid MessageId;
        [ProtoMember(2, IsRequired = true)]
        public readonly PeerId FromPeer;
        [ProtoMember(3, IsRequired = true)]
        public readonly PeerId ToPeer;
        [ProtoMember(4, IsRequired = true)]
        public readonly bool ProcessingSuccessful;
        [ProtoMember(5, IsRequired = true)]
        public readonly IEndpoint Endpoint;
        [ProtoMember(6, IsRequired = true)]
        public readonly string MessageType;

        public ShadowCompletionMessage(Guid messageId, PeerId fromPeer, PeerId toPeer, bool processingSuccessful, IEndpoint endpoint, string messageType)
        {
            MessageId = messageId;
            FromPeer = fromPeer;
            ToPeer = toPeer;
            ProcessingSuccessful = processingSuccessful;
            Endpoint = endpoint;
            MessageType = messageType;
        }
    }
}