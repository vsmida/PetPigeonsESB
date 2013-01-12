using System;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class ShadowMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly MessageWireData Message;
        [ProtoMember(2, IsRequired = true)]
        public readonly string PrimaryRecipient;
        [ProtoMember(3, IsRequired = true)]
        public readonly bool PrimaryWasOnline;
        [ProtoMember(4, IsRequired = true)]
        public readonly IEndpoint TargetEndpoint;

        public ShadowMessageCommand(MessageWireData message, string primaryRecipient, bool primaryWasOnline, IEndpoint targetEndpoint)
        {
            Message = message;
            PrimaryRecipient = primaryRecipient;
            PrimaryWasOnline = primaryWasOnline;
            TargetEndpoint = targetEndpoint;
        }
    }

}
