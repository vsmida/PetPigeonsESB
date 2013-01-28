using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Shadowing
{
    [ProtoContract]
    class ShadowMessageCommand : ICommand
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
